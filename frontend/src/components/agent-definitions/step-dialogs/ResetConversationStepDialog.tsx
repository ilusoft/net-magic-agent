import type { StepDialogBaseProps } from "@/components/agent-definitions/step-dialogs/StepDialogShared";
import { StandardStepDialog } from "@/components/agent-definitions/step-dialogs/StepDialogShared";

export function ResetConversationStepDialog(props: StepDialogBaseProps) {
  return (
    <StandardStepDialog
      {...props}
      showConversationToggle={false}
      showTools={false}
      showParameters={false}
    />
  );
}
