const localApiBaseUrl = "http://localhost:5112";

function normalizeBaseUrl(value: string): string {
  return value.replace(/\/$/, "");
}

export function getBrowserApiBaseUrl(
  environment = process.env.NODE_ENV,
  configuredApiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL,
): string {
  if (environment === "production") {
    return "";
  }

  return normalizeBaseUrl(configuredApiBaseUrl ?? localApiBaseUrl);
}

export function getApiProxyDestination(
  configuredApiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL,
): string {
  return normalizeBaseUrl(configuredApiBaseUrl ?? localApiBaseUrl) + "/api/:path*";
}

export const browserApiBaseUrl = getBrowserApiBaseUrl();
