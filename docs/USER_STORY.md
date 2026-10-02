# User Story

As a registered user, I want to securely manage my personal tasks so that I can keep track of the work I need to complete.

## Acceptance Criteria

- A new user can create an account.
- A registered user can log in with valid credentials.
- An authenticated user can create a task.
- An authenticated user can view only their own tasks.
- An authenticated user can update one of their own tasks.
- An authenticated user can delete one of their own tasks.
- A task contains:
  - title;
  - description;
  - status;
  - due date.
- An unauthenticated user cannot access protected task endpoints.
- One user cannot access or modify another user's tasks.
- Invalid task data is rejected with an appropriate validation response.
- Registration, login, and safe public information are available anonymously.
- After authentication, a user can access protected current-user information; invalid or missing authentication cannot access it.

These criteria describe user behavior; two-host routing is an architecture responsibility documented in PROJECT_DEFINITION.md. M4 implements and tests the HTTP acceptance criteria. M5 implements frontend registration/login, same-tab session restoration and logout. M6 adds the protected task list, create/edit forms, numeric status labels, calendar due dates, confirmed deletion and recoverable errors. M7 final full-system checks remain separate.
