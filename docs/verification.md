# Verification

What was checked, how and when. The README states only what this page shows, or says "not tested yet".

| # | Check | How | Result |
|---|---|---|---|
| V1 | The Terraform root is valid and every module's tests pass | `terraform fmt -check -recursive`, `terraform validate` in `infra/`, `terraform test` in each `infra/modules/<module>` (mock providers) | Passed, 2026-11-20 (Terraform 1.15.8, azurerm 4.81, azapi 2.14): 15 tests in 6 modules |
| V2 | `azd up` provisions everything and deploys the functions; the app keeps `kind: functionapp` after `azd deploy` | `azd up`, then `az containerapp show -n <app> -g <group> --query kind` | not tested yet |
| V3 | The Functions host starts with identity-based host storage (no `AzureWebJobsStorage` connection string) | `az containerapp function list`; no storage errors in the logs | not tested yet |
| V4 | An upload to `receipts` reaches the `receipts-uploaded` queue through Event Grid, with the topic's managed identity | Upload `samples/receipts-2026-11.csv`; Event Grid delivery metrics; queue length | not tested yet |
| V5 | `SummarizeReceipts` reads the Event Grid message as delivered to the queue, and writes the summary row | `GET /api/summaries` after an upload; function invocation traces | not tested yet |
| V6 | The app scales from 0 on queue messages and back to 0 when idle, with the generated scale rules and the managed identity | Replica count over time (`az containerapp replica list`, metrics) | not tested yet |
| V7 | The cost of an idle day, and of a day of use | Cost Management, by meter and day | not tested yet |
