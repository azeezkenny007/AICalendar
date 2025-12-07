# TODO: Add Seq Health Check to /health/detailed Endpoint

## Tasks
- [x] Add private method `CheckSeq()` to perform HTTP request to Seq's health endpoint
- [x] Update `GetDetailed()` method to call `CheckSeq()` and include `seq` in the `checks` object
- [x] Update the `allHealthy` condition to include Seq status
- [x] Update XML documentation in `GetDetailed()` to mention Seq
- [x] Test the health check endpoint after implementation
