# Verification

What was checked, how and when. The README states only what this page shows, or says "not tested yet".

| # | Check | How | Result |
|---|---|---|---|
| V1 | The Terraform root is valid and every module's tests pass | `terraform fmt -check -recursive`, `terraform validate` in `infra/`, `terraform test` in each `infra/modules/<module>` (mock providers) | Passed, 2026-11-20 (Terraform 1.15.8, azurerm 4.81, azapi 2.14): 15 tests in 6 modules |
| V2 | `azd up` provisions everything and deploys the functions; the app keeps `kind: functionapp` after `azd deploy` | `azd up`, then `az containerapp show -n <app> -g <group> --query kind` | Passed, 2026-11-20 (azd 1.38.0, francecentral): `azd up` provisioned 27 resources and deployed in 6 min 24 s after fix D9; `kind` stayed `functionapp`, the app runs the deployed image on port 8080 |
| V3 | The Functions host starts with identity-based host storage (no `AzureWebJobsStorage` connection string) | `az containerapp function list`; no storage errors in the logs | Passed, 2026-11-20: `GET /api/summaries` answered 200 (table read with the managed identity); the host ran with identity-based host storage only |
| V4 | An upload to `receipts` reaches the `receipts-uploaded` queue through Event Grid, with the topic's managed identity | Upload `samples/receipts-2026-11.csv`; Event Grid delivery metrics; queue length | Passed, 2026-11-20: the upload's message reached `receipts-uploaded` and was consumed |
| V5 | `SummarizeReceipts` reads the Event Grid message as delivered to the queue, and writes the summary row | `GET /api/summaries` after an upload; function invocation traces | Passed, 2026-11-20: `samples/receipts-2026-11.csv` summarized about 12 s after the upload (10 lines, 0 malformed, EUR 94.70, GBP 12.35, USD 248.99); the queue trigger read Event Grid's message with the extension's default settings |
| V6 | The app scales from 0 on queue messages and back to 0 when idle, with the generated scale rules and the managed identity | Replica count over time (`az containerapp replica list`, metrics) | not tested yet: the app ran with min 0 replicas; scale-down to 0 and a queue-only scale-up are still to observe |
| V7 | The cost of an idle day, and of a day of use | Cost Management, by meter and day | not tested yet |
