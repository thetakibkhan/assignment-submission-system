# Mock Academic Data — TDD Evidence

## Journey

As an administrator, I want sample classes/courses and subjects after startup so the Academic Structure screens have useful data to manage and update.

## RED and GREEN evidence

- RED: `GetAll_ShouldIncludeMockAcademicData_WhenRequestedByAdmin` failed on a fresh database because the class/course collection was empty.
- GREEN: the same test passed after idempotent seeding was added to `DatabaseInitializer`.
- Full verification: 22 API integration tests and 3 domain tests passed.
- Focused coverage command passed with the `XPlat Code Coverage` collector.

## Guarantee

The initializer adds `CLS-09`, `CLS-10`, `SUB-MAT`, `SUB-ENG`, and `SUB-SCI` only when their codes are absent; existing academic records are preserved.
