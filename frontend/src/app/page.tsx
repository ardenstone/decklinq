"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { getStoredToken } from "@/lib/api";

export default function HomePage() {
  const router = useRouter();

  useEffect(() => {
    router.replace(getStoredToken() ? "/dashboard" : "/login");
  }, [router]);

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-950 text-slate-100">
      <div className="text-center">
        <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">DeckLinq</p>
        <p className="mt-4 text-lg text-slate-300">Redirecting to your deck dashboard...</p>
      </div>
    </main>
  );
}
