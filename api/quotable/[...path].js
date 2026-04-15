const QUOTABLE_BASE = "https://api.quotable.io";
const ZENQUOTES_RANDOM = "https://zenquotes.io/api/random";

const LOCAL_FALLBACK_QUOTES = [
  {
    content: "Success is not final, failure is not fatal: it is the courage to continue that counts.",
    author: "Winston Churchill"
  },
  {
    content: "The best way to predict the future is to create it.",
    author: "Peter Drucker"
  },
  {
    content: "Small steps every day lead to big results.",
    author: "Unknown"
  },
  {
    content: "Do what you can, with what you have, where you are.",
    author: "Theodore Roosevelt"
  }
];

function json(data, status = 200) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { "content-type": "application/json; charset=utf-8" }
  });
}

function fallbackRandomQuote() {
  const selected = LOCAL_FALLBACK_QUOTES[Math.floor(Math.random() * LOCAL_FALLBACK_QUOTES.length)];
  return {
    _id: crypto.randomUUID(),
    content: selected.content,
    author: selected.author,
    tags: []
  };
}

async function fetchWithTimeout(url, ms = 3000) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), ms);
  try {
    return await fetch(url, {
      method: "GET",
      headers: { accept: "application/json" },
      signal: controller.signal
    });
  } finally {
    clearTimeout(timeout);
  }
}

async function fetchFallbackRandom() {
  try {
    const response = await fetchWithTimeout(ZENQUOTES_RANDOM, 3000);
    if (!response.ok) return null;
    const body = await response.json();
    if (!Array.isArray(body) || body.length === 0) return null;
    const first = body[0] ?? {};
    if (!first.q) return null;
    return {
      _id: crypto.randomUUID(),
      content: first.q,
      author: first.a || "Unknown",
      tags: []
    };
  } catch {
    return null;
  }
}

export const config = {
  runtime: "edge"
};

export default async function (request) {
  const url = new URL(request.url);
  const prefix = "/api/quotable/";
  let pathParam = "";
  if (url.pathname.startsWith(prefix)) {
    pathParam = url.pathname.slice(prefix.length).replace(/^\/+/, "");
  }
  const query = url.search || "";
  const upstream = `${QUOTABLE_BASE}/${pathParam}${query}`;
  const isRandom = pathParam.toLowerCase() === "random";

  for (let attempt = 1; attempt <= 3; attempt += 1) {
    try {
      const response = await fetchWithTimeout(upstream, 3000);
      if (!response.ok) throw new Error(`upstream ${response.status}`);
      const contentType = response.headers.get("content-type") || "application/json; charset=utf-8";
      const text = await response.text();
      return new Response(text, { status: 200, headers: { "content-type": contentType } });
    } catch {
      if (attempt < 3) {
        await new Promise((resolve) => setTimeout(resolve, 200 * attempt));
        continue;
      }
    }
  }

  if (isRandom) {
    const fallback = (await fetchFallbackRandom()) || fallbackRandomQuote();
    return json(fallback, 200);
  }

  return json({ error: "Quote service temporarily unavailable. Please try again." }, 502);
}
