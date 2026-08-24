export interface LoginCredentials {
  institutionalId: string;
  password: string;
}

export interface LoginHttpResponse {
  json(): Promise<unknown>;
  ok: boolean;
  status: number;
}

type LoginRequestSender = (
  endpoint: string,
  credentials: LoginCredentials,
) => Promise<LoginHttpResponse>;

interface LoginRetryOptions {
  maxAttempts?: number;
  onServiceStarting: () => void;
  sendRequest?: LoginRequestSender;
  waitBeforeRetry?: () => Promise<void>;
}

const defaultMaximumAttempts = 12;
const retryDelayMilliseconds = 5_000;
const serviceUnavailableStatuses = new Set([502, 503, 504]);

async function sendLoginRequest(
  endpoint: string,
  credentials: LoginCredentials,
): Promise<LoginHttpResponse> {
  return fetch(endpoint, {
    body: JSON.stringify(credentials),
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    method: "POST",
  });
}

function waitForRetry(): Promise<void> {
  return new Promise((resolve) => {
    window.setTimeout(resolve, retryDelayMilliseconds);
  });
}

export function isServiceUnavailableStatus(status: number): boolean {
  return serviceUnavailableStatuses.has(status);
}

export async function submitLoginWithColdStartRetry(
  endpoint: string,
  credentials: LoginCredentials,
  options: LoginRetryOptions,
): Promise<LoginHttpResponse> {
  const maximumAttempts = Math.max(1, options.maxAttempts ?? defaultMaximumAttempts);
  const sendRequest = options.sendRequest ?? sendLoginRequest;
  const waitBeforeRetry = options.waitBeforeRetry ?? waitForRetry;
  let hasReportedServiceStarting = false;

  for (let attempt = 1; attempt <= maximumAttempts; attempt += 1) {
    try {
      const response = await sendRequest(endpoint, credentials);

      if (!isServiceUnavailableStatus(response.status) || attempt === maximumAttempts) {
        return response;
      }
    } catch (error) {
      if (attempt === maximumAttempts) {
        throw error;
      }
    }

    if (!hasReportedServiceStarting) {
      options.onServiceStarting();
      hasReportedServiceStarting = true;
    }

    await waitBeforeRetry();
  }

  throw new Error("Sign-in retry attempts were exhausted.");
}
