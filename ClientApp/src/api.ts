import type {
  LocationSuggestion,
  SolicitorProfileResponse,
  SolicitorSearchDefaultsResponse,
  SolicitorSearchRequest,
  SolicitorSearchResponse
} from "./types";

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    headers: {
      "Content-Type": "application/json",
      ...init?.headers
    },
    ...init
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `Request failed with status ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export function getDefaults(): Promise<SolicitorSearchDefaultsResponse> {
  return request<SolicitorSearchDefaultsResponse>("/api/solicitors/conveyancing/defaults");
}

export function searchSolicitors(requestBody: SolicitorSearchRequest): Promise<SolicitorSearchResponse> {
  return request<SolicitorSearchResponse>("/api/solicitors/conveyancing/search", {
    method: "POST",
    body: JSON.stringify(requestBody)
  });
}

export function getLocationSuggestions(query: string): Promise<LocationSuggestion[]> {
  return request<LocationSuggestion[]>(
    `/api/solicitors/locations/suggestions?query=${encodeURIComponent(query)}`
  );
}

export function getSolicitorProfile(slug: string): Promise<SolicitorProfileResponse> {
  return request<SolicitorProfileResponse>(`/api/solicitors/${encodeURIComponent(slug)}`);
}
