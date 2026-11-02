# Clean architecture verification

- Four net10.0 projects; Domain and Application have no ASP.NET or Npgsql references.
- SQL, SMTP, Ollama and curriculum loading are adapter responsibilities. UI resolves application contracts; adapter wiring is confined to Configuration.
- Learning rules and XP are shared by lobby and profile. Exercise inputs use Parameter/EventCallback. Existing DOM IDs, CSS and JSON property names are retained.
- Legacy file storage and username login removed. Shared account transaction locks, database clock and credential revocation extracted without changing transaction boundaries.
- 47 unit tests passed. Persistence QA passed isolation, JSON roundtrip, stale revision rejection and draft preservation.
- Existing SMTP/browser suite passed 60 checks without JavaScript errors; no real email was sent.
- Docker image build and publish asset assertions passed.
- PostgreSQL custom-format backup saved privately in ILearn/App_Data/backups/before-clean-architecture.dump, then successfully restored to ilearn_refactor_qa.
- Cleanup first passed on restored QA data, then on the main database: 14 selected profiles and 13 progress records deleted; only ilearn_tester retained. Transaction assertions verified unchanged retained profile, password hash and progress.
- Cleanup SQL is manual and restricted to the reviewed UUIDs; it is never invoked at startup.
- NuGet vulnerability lookup was unavailable during local builds (NU1900); compilation and tests succeeded.

The production app container has not been redeployed. The isolated QA container used port 5082 and was stopped after verification. Historical verification reports describe their original versions.
