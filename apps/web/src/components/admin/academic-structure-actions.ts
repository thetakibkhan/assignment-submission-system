export const academicStructureActions = [
  { id: "create", label: "Create structure" },
  { id: "manage", label: "Manage structure" },
  { id: "update", label: "Update structure" },
] as const;

export type AcademicStructureAction = (typeof academicStructureActions)[number]["id"];

export function getAcademicStructurePanelVisibility(
  selectedAction: AcademicStructureAction | null,
): Record<AcademicStructureAction, boolean> {
  return {
    create: selectedAction === "create",
    manage: selectedAction === "manage",
    update: selectedAction === "update",
  };
}
