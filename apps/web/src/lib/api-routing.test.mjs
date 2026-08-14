import assert from "node:assert/strict";
import test from "node:test";

import {
  getBrowserApiBaseUrl,
  getApiProxyDestination,
} from "./api-routing.ts";

test("production browser requests stay on the web origin", () => {
  assert.equal(
    getBrowserApiBaseUrl(
      "production",
      "https://assignment-submission-system-api.onrender.com",
    ),
    "",
  );
});

test("development browser requests use the configured API origin", () => {
  assert.equal(
    getBrowserApiBaseUrl("development", "http://localhost:5112/"),
    "http://localhost:5112",
  );
});

test("the web proxy forwards API paths to the configured backend", () => {
  assert.equal(
    getApiProxyDestination(
      "https://assignment-submission-system-api.onrender.com/",
    ),
    "https://assignment-submission-system-api.onrender.com/api/:path*",
  );
});
