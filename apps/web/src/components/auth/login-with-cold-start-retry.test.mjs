import assert from "node:assert/strict";
import test from "node:test";

import {
  isServiceUnavailableStatus,
  submitLoginWithColdStartRetry,
} from "./login-with-cold-start-retry.ts";

function createResponse(status) {
  return {
    json: async () => ({}),
    ok: status >= 200 && status < 300,
    status,
  };
}

test("retries transient gateway failures and reports that the service is starting", async () => {
  const responses = [createResponse(502), createResponse(503), createResponse(200)];
  let requestCount = 0;
  let serviceStartingCount = 0;
  let waitCount = 0;

  const response = await submitLoginWithColdStartRetry(
    "/api/auth/login",
    { institutionalId: "DEMO-ADM-001", password: "public-demo-password" },
    {
      maxAttempts: 3,
      onServiceStarting: () => {
        serviceStartingCount += 1;
      },
      sendRequest: async () => {
        const responseForAttempt = responses[requestCount];
        requestCount += 1;
        return responseForAttempt;
      },
      waitBeforeRetry: async () => {
        waitCount += 1;
      },
    },
  );

  assert.equal(response.status, 200);
  assert.equal(requestCount, 3);
  assert.equal(serviceStartingCount, 1);
  assert.equal(waitCount, 2);
});

test("does not retry an invalid-credentials response", async () => {
  let requestCount = 0;
  let serviceStartingCount = 0;

  const response = await submitLoginWithColdStartRetry(
    "/api/auth/login",
    { institutionalId: "UNKNOWN", password: "incorrect" },
    {
      maxAttempts: 3,
      onServiceStarting: () => {
        serviceStartingCount += 1;
      },
      sendRequest: async () => {
        requestCount += 1;
        return createResponse(401);
      },
      waitBeforeRetry: async () => {},
    },
  );

  assert.equal(response.status, 401);
  assert.equal(requestCount, 1);
  assert.equal(serviceStartingCount, 0);
});

test("retries a temporary network failure", async () => {
  let requestCount = 0;

  const response = await submitLoginWithColdStartRetry(
    "/api/auth/login",
    { institutionalId: "DEMO-ADM-001", password: "public-demo-password" },
    {
      maxAttempts: 2,
      onServiceStarting: () => {},
      sendRequest: async () => {
        requestCount += 1;

        if (requestCount === 1) {
          throw new TypeError("Network request failed");
        }

        return createResponse(200);
      },
      waitBeforeRetry: async () => {},
    },
  );

  assert.equal(response.status, 200);
  assert.equal(requestCount, 2);
});

test("recognizes only temporary upstream status codes as service unavailability", () => {
  assert.equal(isServiceUnavailableStatus(502), true);
  assert.equal(isServiceUnavailableStatus(503), true);
  assert.equal(isServiceUnavailableStatus(504), true);
  assert.equal(isServiceUnavailableStatus(400), false);
  assert.equal(isServiceUnavailableStatus(401), false);
  assert.equal(isServiceUnavailableStatus(429), false);
});
