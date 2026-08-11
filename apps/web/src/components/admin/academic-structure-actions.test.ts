import assert from "node:assert/strict";
import test from "node:test";

import {
  academicStructureActions,
  getAcademicStructurePanelVisibility,
} from "./academic-structure-actions.ts";

test("academic structure shows no workflow before an action is selected", () => {
  const visibility = getAcademicStructurePanelVisibility(null);

  assert.equal(Object.values(visibility).filter(Boolean).length, 0);
});

test("academic structure shows only the selected workflow", () => {
  for (const action of academicStructureActions) {
    const visibility = getAcademicStructurePanelVisibility(action.id);

    assert.equal(visibility[action.id], true);
    assert.equal(Object.values(visibility).filter(Boolean).length, 1);
  }
});
