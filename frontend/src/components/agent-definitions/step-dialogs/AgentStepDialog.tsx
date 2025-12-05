import type { StepDialogBaseProps } from "@/components/agent-definitions/step-dialogs/StepDialogShared";
import { StandardStepDialog } from "@/components/agent-definitions/step-dialogs/StepDialogShared";

export function AgentStepDialog(props: StepDialogBaseProps) {
  return <StandardStepDialog {...props} />;
}
