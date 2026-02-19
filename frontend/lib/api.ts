const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api";

function authHeaders(token?: string | null): Record<string, string> {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };
  if (token) headers["Authorization"] = `Bearer ${token}`;
  return headers;
}

export const api = {
  get: async (endpoint: string, token?: string | null) => {
    const res = await fetch(`${API_URL}${endpoint}`, {
      headers: authHeaders(token),
    });
    if (!res.ok) throw new Error("API request failed");
    return res.json();
  },

  post: async (endpoint: string, data: unknown, token?: string | null) => {
    const res = await fetch(`${API_URL}${endpoint}`, {
      method: "POST",
      headers: authHeaders(token),
      body: JSON.stringify(data),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      const error = Object.assign(new Error("API request failed"), {
        status: res.status,
        data: err,
      });
      throw error;
    }
    return res.json();
  },

  put: async (endpoint: string, data: unknown, token?: string | null) => {
    const res = await fetch(`${API_URL}${endpoint}`, {
      method: "PUT",
      headers: authHeaders(token),
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error("API request failed");
    return res.json();
  },

  delete: async (endpoint: string, token?: string | null) => {
    const res = await fetch(`${API_URL}${endpoint}`, {
      method: "DELETE",
      headers: authHeaders(token),
    });
    if (!res.ok) throw new Error("API request failed");
    return res.json();
  },
};

// Mirrors the backend ReadingStatus enum
export const ReadingStatus = {
  WantToRead: 1,
  Read: 2,
} as const;
export type ReadingStatusValue =
  (typeof ReadingStatus)[keyof typeof ReadingStatus];
