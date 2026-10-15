import createClient from "openapi-fetch";
import type { paths } from "./schema";

// The AppHost sets API_URL; the fallback is the API's own launch profile.
export const api = createClient<paths>({ baseUrl: process.env.API_URL ?? "http://localhost:5080" });
