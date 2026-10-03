# Assessment Requirements

## Authority and classification

The authoritative external source is **Net - BLA - Technical Interview Exercise - V5.pdf**. This canonical transcription was originally prepared from the complete requirements supplied during the reconciliation checkpoint, without independent agent inspection or committing the proprietary PDF. It has since been manually cross-checked against the original assessment outside the repository. The original assessment remains authoritative if a discrepancy is found; the PDF is not tracked and is not a submission dependency.

Mandatory requirements below preserve the assessment's meaning. Evaluation criteria describe how the submission is assessed. TDD is an expected methodology, with test-first practice preferred where practical; no fabricated chronology is acceptable. Project-specific choices (SQLite, Angular 22, JWT, ownership response policy, two executable hosts, controller pipeline) are recorded in PROJECT_DEFINITION.md and DECISIONS.md rather than attributed verbatim to the assessment. REQUIREMENTS_TRACEABILITY.md tracks evidence and remaining work; it cannot weaken requirements.

## Project overview — mandatory

- Develop a simple web application with .NET/C#, ASP.NET MVC, Web API, and a database or other data store.
- Follow Clean Architecture principles and use TDD methodologies (see the methodology qualification above).
- Drive development from an informal user story created by the developer and include that story in the presentation.
- Users can create, read, update, and delete application records through API endpoints.
- Support creating a user and logging in as that user; store user information in the data store.
- Entity Framework, Dapper, and Mediator/MediatR are prohibited. Do not reproduce a mediator or ORM through custom abstractions.

## Backend — database — mandatory

- At least one table/object/container for application data.
- An additional table/object/container for users.
- The application data object has a unique identifier/primary key and at least two other fields.

## Backend — API — mandatory

- An ASP.NET Web API exposes CRUD operations on application data.
- Use appropriate HTTP verbs, parameters, and return values.
- Additionally, a **SECOND API** includes endpoints for user creation, user login, authorized behavior, and non-authorized behavior.
- Do not collapse the second API into an authentication controller on the first host. The developer-approved conservative implementation is two separate executable ASP.NET Core hosts: TaskManager.Api and TaskManager.Auth.Api.

## Backend — data layer — mandatory

- Provide a data access layer interacting with persistence and performing CRUD required by the API.

## Backend — business logic — mandatory

- Provide business rules and validation in a business logic layer independent of the data layer implementation and the API.

## Backend — tests and methodology

- Mandatory: test all relevant components, including data access, business logic, and API endpoints.
- Expected/preferred: TDD, using observed failing/passing tests for important behavior where practical.
- Do not claim test-first history that did not occur. Tooling failures before assertions are not evidence of a failing behavior test.

## Frontend — mandatory

- Integrate the backend with a frontend framework.
- Provide responsive, user-friendly UI implementing CRUD for the chosen use case.
- Organize components and state cleanly.

## Submission — mandatory

- Include README setup instructions and other necessary documentation.
- Provide seeded data and demo credentials.

## Generative AI portion — mandatory deliverables

Demonstrate a prompt generating a RESTful task-management API supporting create, read, update, and delete. Tasks contain **title, description, status, due_date**, and belong to a user.

Include:

1. The prompt used or a clearly labeled representative prompt.
2. Output code or a representative sample.
3. How AI suggestions were validated.
4. Corrections and improvements.
5. Edge cases.
6. Authentication handling.
7. Validation handling.

Record real evidence. A representative HTTP API sample that does not exist yet must remain pending, even when application/persistence samples already exist.

## Presentation and code review — mandatory content

Explain the informal user story, design choices, technical architecture, and application functionality.

## Evaluation criteria

- Clean Architecture and separation of concerns.
- Sufficient testing and TDD.
- Organized, readable code.
- Correct functionality.
- Frontend quality.
- Presentation quality.
- GenAI fluency.
- Prompt engineering.
- Critical evaluation of AI-generated work.

## Optional/desirable items

No additional optional product features are specified in the developer-supplied assessment text. Project enhancements are not substitutes for mandatory requirements. Additional frontend tests are desirable in the project plan; backend component/endpoint testing remains mandatory. No refresh tokens, Razor views, or microservice infrastructure are required by these requirements.
