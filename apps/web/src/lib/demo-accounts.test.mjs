import assert from "node:assert/strict";
import test from "node:test";

import { recruiterDemoAccounts } from "./demo-accounts.ts";

test("recruiter demo accounts use public IDs that do not conflict with private accounts", () => {
  assert.deepEqual(
    recruiterDemoAccounts.map(({ institutionalId, role }) => ({ institutionalId, role })),
    [
      { institutionalId: "DEMO-ADM-001", role: "Administrator" },
      { institutionalId: "DEMO-TCH-001", role: "Teacher" },
      { institutionalId: "DEMO-STU-001", role: "Student" },
    ],
  );
});

test("every recruiter demo account has a strong public demo password", () => {
  for (const account of recruiterDemoAccounts) {
    assert.match(account.password, /[A-Z]/);
    assert.match(account.password, /[a-z]/);
    assert.match(account.password, /\d/);
    assert.match(account.password, /[^A-Za-z0-9]/);
    assert.ok(account.password.length >= 16);
  }
});
