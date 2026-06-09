import { OperatorHome } from "./operator";
import { LearnerHome } from "./learner-home";

// Thin server shell: awaits searchParams (Next.js 16 — it is a Promise) and branches role. The learner
// home is a client component because it fetches live enrollment data via the in-memory access token.
export default async function CuaBanPage({ searchParams }: { searchParams: Promise<{ role?: string }> }) {
  const sp = await searchParams;
  return sp.role === "operator" ? <OperatorHome /> : <LearnerHome />;
}
