export type AuthUser = {
  id: number;
  username: string;
  createdAt: string;
};

export type Card = {
  id: number;
  deckId: number;
  frontContent: string;
  backContent: string;
  // New fields for question types
  questionType?: string | null;
  metadata?: Record<string, any> | null;
  hints?: string | null;
  isLaTeX: boolean;
  isArchived?: boolean;
  archivedAt?: string | null;
  createdAt: string;
};

export type Deck = {
  id: number;
  userId: number;
  title: string;
  description: string;
  isPublic: boolean;
  cards: Card[];
  cardCount: number;
  createdAt: string;
  updatedAt: string;
};

const apiBase = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

export function getStoredToken() {
  if (typeof window === "undefined") {
    return null;
  }

  return localStorage.getItem("decklinq.token");
}

export function setStoredToken(token: string) {
  if (typeof window === "undefined") {
    return;
  }

  localStorage.setItem("decklinq.token", token);
}

export function clearStoredToken() {
  if (typeof window === "undefined") {
    return;
  }

  localStorage.removeItem("decklinq.token");
}

export async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getStoredToken();

  const response = await fetch(`${apiBase}${path}`, {
    ...options,
    headers: {
      Accept: "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(options.headers ?? {}),
    },
  });

  if (response.status === 401) {
    clearStoredToken();
    throw new Error("Unauthorized");
  }

  if (!response.ok) {
    let message = "Request failed";
    const rawBody = await response.text();

    try {
      const payload = JSON.parse(rawBody) as { message?: string; error?: string };
      message = payload.message ?? payload.error ?? message;
    } catch {
      message = rawBody || message;
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export async function downloadApiFile(path: string, fileName: string, options: RequestInit = {}) {
  const token = getStoredToken();
  const response = await fetch(`${apiBase}${path}`, {
    ...options,
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(options.headers ?? {}),
    },
  });

  if (response.status === 401) {
    clearStoredToken();
    throw new Error("Unauthorized");
  }

  if (!response.ok) {
    let message = "Request failed";
    const rawBody = await response.text();

    try {
      const payload = JSON.parse(rawBody) as { message?: string; error?: string };
      message = payload.message ?? payload.error ?? message;
    } catch {
      message = rawBody || message;
    }

    throw new Error(message);
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}
