# Academic Structure Actions — TDD Evidence

## Source and journey

No source plan was provided. The journey was derived from the requested dashboard change:

> As an administrator, I want separate Create, Manage, and Update actions in Academic Structure so only the selected workflow is visible.

## RED evidence

- Test: `apps/web/src/components/admin/academic-structure-actions.test.mjs`
- Command: `node --test apps/web/src/components/admin/academic-structure-actions.test.ts`
- Result: failed with `ERR_MODULE_NOT_FOUND` because the Academic Structure action model did not exist.
- Checkpoint: `395abff test: define academic workflow visibility`

## GREEN evidence

- Command: `node --test --experimental-test-coverage apps/web/src/components/admin/academic-structure-actions.test.mjs`
- Result: 2 passed, 0 failed; action model coverage is 100% for lines, branches, and functions.
- TypeScript: `./apps/web/node_modules/.bin/tsc -p apps/web/tsconfig.json --noEmit` passed.
- Lint: `npm --prefix apps/web run lint` passed.
- Live route: `http://localhost:3000/admin` returned HTTP 200.

## Guarantees

| What is guaranteed | Test type | Result |
|---|---|---|
| No Academic Structure workflow is visible before an action is selected. | Unit | Pass |
| Selecting Create, Manage, or Update marks exactly one matching workflow as visible. | Unit | Pass |

## Known gap

The project does not currently have a browser component-test runner. The pure visibility model is covered, while the live route, TypeScript, and lint checks validate its integration into the dashboard.
