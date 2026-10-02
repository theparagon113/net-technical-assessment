# TaskManagerWeb

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 22.2.1.

M5 implements authentication; M6 provides protected `/tasks` with list/create/edit/status/due-date/delete UI. Start both hosts using the [root setup instructions](../../../README.md). Public `src/app/core/api-config.ts` configures auth port 5150 and task port 5149 separately. Only JWT is persisted in sessionStorage; refresh confirms identity through `/api/auth/me`, and logout clears the session. Task CRUD uses only Task.Api and the existing JWT interceptor.

Use `npm ci`, `npm start`, `npm run build`, and `npm test -- --watch=false`. Normal tests skip two live probes. With both real hosts using a disposable shared database, set PowerShell `$env:M5_LIVE='1'` and `$env:M6_LIVE='1'` before tests and remove both afterward. See root README and [M6 evidence](../../../docs/M6_COMPLETION.md) for trade-offs, setup and results. No e2e framework is configured yet.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
