import { useCallback, useMemo, useState } from "react";
import type { ChangeEvent, FormEvent } from "react";

import type {
  AgentDefinitionsDocument,
  AgentStepDefinition,
  AgentStepOutcomeDefinition,
} from "../../../types/agents";
import type { OutcomeFormState, WorkflowEdge } from "../types";
import {
  createKeyValueEntry,
  entriesFromRecord,
  recordFromEntries,
} from "../util";

type ApplyDocumentUpdate = (
  updater: (draft: AgentDefinitionsDocument) => AgentDefinitionsDocument | void
) => void;

interface UseOutcomeDialogOptions {
  draftDocument: AgentDefinitionsDocument | null;
  activeWorkflowId: string | null;
  applyDocumentUpdate: ApplyDocumentUpdate;
}

interface OutcomeDialogBindings {
  open: boolean;
  mode: "create" | "edit";
  title: string;
  outcomeForm: OutcomeFormState | null;
  outcomeFormError: string | null;
  availableSteps: string[];
  onClose: () => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onFieldChange: (
    field: "name" | "nextStep" | "conditionType" | "order"
  ) => (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => void;
  onEndWorkflowToggle: (event: ChangeEvent<HTMLInputElement>) => void;
  onAddConditionParameter: () => void;
  onRemoveConditionParameter: (entryId: string) => void;
  onConditionParameterChange: (
    entryId: string,
    field: "key" | "value"
  ) => (event: ChangeEvent<HTMLInputElement>) => void;
  onDelete?: () => void;
}

interface UseOutcomeDialogResult {
  dialogProps: OutcomeDialogBindings;
  openForEdge: (edge: WorkflowEdge) => void;
  openForCreation: (
    sourceStep: string,
    overrides?: Partial<OutcomeFormState>
  ) => void;
  reset: () => void;
}

export function useOutcomeDialog({
  draftDocument,
  activeWorkflowId,
  applyDocumentUpdate,
}: UseOutcomeDialogOptions): UseOutcomeDialogResult {
  const [isOpen, setIsOpen] = useState(false);
  const [mode, setMode] = useState<"create" | "edit">("edit");
  const [dialogTarget, setDialogTarget] = useState<WorkflowEdge | null>(null);
  const [outcomeForm, setOutcomeForm] = useState<OutcomeFormState | null>(null);
  const [outcomeFormError, setOutcomeFormError] = useState<string | null>(null);

  const availableSteps = useMemo(() => {
    if (!draftDocument || !activeWorkflowId) {
      return [] as string[];
    }

    const agent = draftDocument.agents.find(
      (candidate) => candidate.id === activeWorkflowId
    );
    return agent ? agent.steps.map((step) => step.name) : [];
  }, [draftDocument, activeWorkflowId]);

  const reset = useCallback(() => {
    setIsOpen(false);
    setMode("edit");
    setDialogTarget(null);
    setOutcomeForm(null);
    setOutcomeFormError(null);
  }, []);

  const buildOutcomeFormState = useCallback(
    (
      sourceStep: string,
      outcome: AgentStepOutcomeDefinition | null,
      options?: { defaultOrder?: number }
    ): OutcomeFormState => {
      const resolvedOrder =
        outcome?.order !== undefined && outcome.order > 0
          ? outcome.order
          : options?.defaultOrder;

      return {
        sourceStep,
        name: outcome?.name ?? "",
        nextStep: outcome?.nextStep ?? "",
        endWorkflow: outcome?.endWorkflow ?? false,
        conditionType: outcome?.condition?.type ?? "",
        conditionParameters: entriesFromRecord(outcome?.condition?.parameters),
        order: resolvedOrder ? String(resolvedOrder) : "",
      };
    },
    []
  );

  const computeNormalizedOrder = useCallback(
    (
      candidate: AgentStepOutcomeDefinition | null | undefined,
      index: number
    ) => {
      if (candidate?.order && candidate.order > 0) {
        return candidate.order;
      }

      return index + 1;
    },
    []
  );

  const computeNextOrderForStep = useCallback(
    (step: AgentStepDefinition) => {
      if (!Array.isArray(step.outcomes) || step.outcomes.length === 0) {
        return 1;
      }

      const normalizedOrders = step.outcomes.map((candidate, index) =>
        computeNormalizedOrder(candidate, index)
      );

      return Math.max(...normalizedOrders) + 1;
    },
    [computeNormalizedOrder]
  );

  const ensureUniqueOutcomeOrders = useCallback(
    (step: AgentStepDefinition, priorityOutcomeName: string) => {
      if (!Array.isArray(step.outcomes) || step.outcomes.length === 0) {
        return;
      }

      const positiveOrder = (
        candidate: AgentStepOutcomeDefinition | null | undefined
      ) =>
        candidate?.order && candidate.order > 0 ? candidate.order : undefined;

      let maxOrder = step.outcomes.reduce((highest, candidate) => {
        const candidateOrder = positiveOrder(candidate);
        return candidateOrder && candidateOrder > highest
          ? candidateOrder
          : highest;
      }, 0);

      const claimedOrders = new Set<number>();

      const assignNextOrder = (outcome: AgentStepOutcomeDefinition | null) => {
        if (!outcome) {
          return;
        }

        maxOrder = Math.max(maxOrder, 0) + 1;
        while (claimedOrders.has(maxOrder)) {
          maxOrder += 1;
        }

        outcome.order = maxOrder;
        claimedOrders.add(maxOrder);
      };

      const claimExistingOrder = (
        outcome: AgentStepOutcomeDefinition | null
      ): boolean => {
        if (!outcome) {
          return false;
        }

        const desired = positiveOrder(outcome);
        if (desired && !claimedOrders.has(desired)) {
          claimedOrders.add(desired);
          if (desired > maxOrder) {
            maxOrder = desired;
          }
          return true;
        }

        return false;
      };

      const priorityOutcome = step.outcomes.find(
        (candidate) => candidate?.name === priorityOutcomeName
      );

      if (priorityOutcome) {
        if (!claimExistingOrder(priorityOutcome)) {
          assignNextOrder(priorityOutcome);
        }
      }

      for (const outcome of step.outcomes) {
        if (!outcome) {
          continue;
        }

        if (priorityOutcome && outcome === priorityOutcome) {
          continue;
        }

        if (claimExistingOrder(outcome)) {
          continue;
        }

        assignNextOrder(outcome);
      }
    },
    []
  );

  const openForEdge = useCallback(
    (edge: WorkflowEdge) => {
      setDialogTarget(edge);

      if (!draftDocument || !activeWorkflowId) {
        setOutcomeForm(null);
        setOutcomeFormError("Unable to update outcome — missing context.");
        setMode("edit");
        setIsOpen(true);
        return;
      }

      const sourceStep = edge.data?.sourceStep;
      if (!sourceStep) {
        setOutcomeForm(null);
        setOutcomeFormError(
          "Unable to determine source step for this outcome."
        );
        setMode("edit");
        setIsOpen(true);
        return;
      }

      const agent = draftDocument.agents.find(
        (candidate) => candidate.id === activeWorkflowId
      );
      if (!agent) {
        setOutcomeForm(null);
        setOutcomeFormError("Unable to locate agent for this workflow.");
        setMode("edit");
        setIsOpen(true);
        return;
      }

      const step = agent.steps.find(
        (candidate) => candidate.name === sourceStep
      );
      if (!step) {
        setOutcomeForm(null);
        setOutcomeFormError("Unable to locate step for this outcome.");
        setMode("edit");
        setIsOpen(true);
        return;
      }

      const existingOutcomeIndex = step.outcomes?.findIndex(
        (candidate) => candidate.name === edge.data?.outcomeName
      );
      const existingOutcome =
        existingOutcomeIndex !== undefined && existingOutcomeIndex >= 0
          ? step.outcomes?.[existingOutcomeIndex] ?? null
          : null;
      setMode(existingOutcome ? "edit" : "create");
      setOutcomeForm(
        buildOutcomeFormState(sourceStep, existingOutcome ?? null, {
          defaultOrder:
            existingOutcomeIndex !== undefined && existingOutcomeIndex >= 0
              ? existingOutcomeIndex + 1
              : undefined,
        })
      );
      setOutcomeFormError(null);
      setIsOpen(true);
    },
    [draftDocument, activeWorkflowId, buildOutcomeFormState]
  );

  const openForCreation = useCallback(
    (sourceStep: string, overrides?: Partial<OutcomeFormState>) => {
      setDialogTarget(null);

      if (!draftDocument || !activeWorkflowId) {
        setOutcomeForm(null);
        setOutcomeFormError("Unable to create outcome — missing context.");
        setMode("create");
        setIsOpen(true);
        return;
      }

      const agent = draftDocument.agents.find(
        (candidate) => candidate.id === activeWorkflowId
      );

      if (!agent) {
        setOutcomeForm(null);
        setOutcomeFormError("Unable to locate agent for this workflow.");
        setMode("create");
        setIsOpen(true);
        return;
      }

      const step = agent.steps.find(
        (candidate) => candidate.name === sourceStep
      );

      if (!step) {
        setOutcomeForm(null);
        setOutcomeFormError("Unable to locate the selected source step.");
        setMode("create");
        setIsOpen(true);
        return;
      }

      const nextOrder = computeNextOrderForStep(step);
      const baseForm = buildOutcomeFormState(step.name, null, {
        defaultOrder: nextOrder,
      });
      const mergedForm: OutcomeFormState = {
        ...baseForm,
        ...overrides,
        conditionParameters:
          overrides?.conditionParameters ?? baseForm.conditionParameters,
        order: overrides?.order ?? baseForm.order,
      };

      setMode("create");
      setOutcomeForm(mergedForm);
      setOutcomeFormError(null);
      setIsOpen(true);
    },
    [draftDocument, activeWorkflowId, buildOutcomeFormState]
  );

  const handleFieldChange = useCallback(
    (field: "name" | "nextStep" | "conditionType" | "order") =>
      (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
        const value = event.target.value;
        setOutcomeForm((previous) =>
          previous ? { ...previous, [field]: value } : previous
        );
      },
    []
  );

  const handleEndWorkflowToggle = useCallback(
    (event: ChangeEvent<HTMLInputElement>) => {
      const checked = event.target.checked;
      setOutcomeForm((previous) =>
        previous
          ? {
              ...previous,
              endWorkflow: checked,
              nextStep: checked ? "" : previous.nextStep,
            }
          : previous
      );
    },
    []
  );

  const handleAddConditionParameter = useCallback(() => {
    setOutcomeForm((previous) =>
      previous
        ? {
            ...previous,
            conditionParameters: [
              ...previous.conditionParameters,
              createKeyValueEntry(),
            ],
          }
        : previous
    );
  }, []);

  const handleRemoveConditionParameter = useCallback((entryId: string) => {
    setOutcomeForm((previous) => {
      if (!previous) {
        return previous;
      }

      return {
        ...previous,
        conditionParameters: previous.conditionParameters.filter(
          (entry) => entry.id !== entryId
        ),
      };
    });
  }, []);

  const handleConditionParameterChange = useCallback(
    (entryId: string, field: "key" | "value") =>
      (event: ChangeEvent<HTMLInputElement>) => {
        const value = event.target.value;
        setOutcomeForm((previous) => {
          if (!previous) {
            return previous;
          }

          return {
            ...previous,
            conditionParameters: previous.conditionParameters.map((entry) =>
              entry.id === entryId ? { ...entry, [field]: value } : entry
            ),
          };
        });
      },
    []
  );

  const handleSubmit = useCallback(
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();

      if (!outcomeForm || !draftDocument || !activeWorkflowId) {
        setOutcomeFormError("Unable to update outcome — missing context.");
        return;
      }

      const trimmedName = outcomeForm.name.trim();

      if (!trimmedName) {
        setOutcomeFormError("Outcome name is required.");
        return;
      }

      if (!outcomeForm.endWorkflow && !outcomeForm.nextStep.trim()) {
        setOutcomeFormError(
          "Select a next step or mark the outcome as ending the workflow."
        );
        return;
      }

      const trimmedOrder = outcomeForm.order.trim();
      const parsedOrder = Number.parseInt(trimmedOrder, 10);

      if (!trimmedOrder || Number.isNaN(parsedOrder) || parsedOrder <= 0) {
        setOutcomeFormError("Outcome order must be a positive integer.");
        return;
      }

      applyDocumentUpdate((draft) => {
        const agent = draft.agents.find(
          (candidate) => candidate.id === activeWorkflowId
        );

        if (!agent) {
          return draft;
        }

        const step = agent.steps.find(
          (candidate) => candidate.name === outcomeForm.sourceStep
        );

        if (!step) {
          return draft;
        }

        step.outcomes = step.outcomes ?? [];

        const conditionParameters = recordFromEntries(
          outcomeForm.conditionParameters
        );
        const hasConditionParameters =
          Object.keys(conditionParameters).length > 0;
        const condition =
          outcomeForm.conditionType.trim() || hasConditionParameters
            ? {
                type: outcomeForm.conditionType.trim() || undefined,
                parameters: hasConditionParameters
                  ? conditionParameters
                  : undefined,
              }
            : undefined;

        const updatedOutcome: AgentStepOutcomeDefinition = {
          name: trimmedName,
          nextStep: outcomeForm.endWorkflow
            ? undefined
            : outcomeForm.nextStep.trim() || undefined,
          endWorkflow: outcomeForm.endWorkflow || undefined,
          condition,
          order: parsedOrder,
        };

        if (!updatedOutcome.endWorkflow) {
          delete (updatedOutcome as Partial<AgentStepOutcomeDefinition>)
            .endWorkflow;
        }

        if (
          updatedOutcome.condition &&
          (!updatedOutcome.condition.parameters ||
            Object.keys(updatedOutcome.condition.parameters).length === 0)
        ) {
          delete updatedOutcome.condition.parameters;
        }

        if (mode === "edit") {
          const existingIndex = step.outcomes.findIndex(
            (candidate) =>
              candidate.name ===
              (dialogTarget?.data?.outcomeName ?? trimmedName)
          );

          if (existingIndex >= 0) {
            step.outcomes[existingIndex] = updatedOutcome;
          } else {
            step.outcomes.push(updatedOutcome);
          }
        } else {
          step.outcomes.push(updatedOutcome);
        }

        ensureUniqueOutcomeOrders(step, trimmedName);

        step.outcomes = step.outcomes.map((candidate) => ({
          ...candidate,
          name: candidate.name.trim(),
        }));

        return draft;
      });

      setOutcomeFormError(null);
      reset();
    },
    [
      outcomeForm,
      draftDocument,
      activeWorkflowId,
      mode,
      dialogTarget,
      applyDocumentUpdate,
      reset,
      computeNormalizedOrder,
      ensureUniqueOutcomeOrders,
    ]
  );

  const handleDelete = useCallback(() => {
    if (
      !draftDocument ||
      !activeWorkflowId ||
      !dialogTarget?.data?.outcomeName ||
      !dialogTarget.data?.sourceStep
    ) {
      setOutcomeFormError("Unable to delete outcome — missing context.");
      return;
    }

    applyDocumentUpdate((draft) => {
      const agent = draft.agents.find(
        (candidate) => candidate.id === activeWorkflowId
      );

      if (!agent) {
        return draft;
      }

      const step = agent.steps.find(
        (candidate) => candidate.name === dialogTarget.data!.sourceStep
      );

      if (!step || !Array.isArray(step.outcomes)) {
        return draft;
      }

      step.outcomes = step.outcomes.filter(
        (candidate) => candidate.name !== dialogTarget.data!.outcomeName
      );

      return draft;
    });

    setOutcomeFormError(null);
    reset();
  }, [
    draftDocument,
    activeWorkflowId,
    dialogTarget,
    applyDocumentUpdate,
    reset,
  ]);

  const title = useMemo(() => {
    if (mode === "create") {
      return "Create Outcome";
    }

    if (!dialogTarget) {
      return "Configure Outcome";
    }

    return `Configure Outcome “${
      dialogTarget.data?.outcomeName ?? dialogTarget.label ?? "outcome"
    }”`;
  }, [mode, dialogTarget]);

  return {
    dialogProps: {
      open: isOpen,
      mode,
      title,
      outcomeForm,
      outcomeFormError,
      availableSteps,
      onClose: reset,
      onSubmit: handleSubmit,
      onFieldChange: handleFieldChange,
      onEndWorkflowToggle: handleEndWorkflowToggle,
      onAddConditionParameter: handleAddConditionParameter,
      onRemoveConditionParameter: handleRemoveConditionParameter,
      onConditionParameterChange: handleConditionParameterChange,
      onDelete: mode === "edit" ? handleDelete : undefined,
    },
    openForEdge,
    openForCreation,
    reset,
  };
}
