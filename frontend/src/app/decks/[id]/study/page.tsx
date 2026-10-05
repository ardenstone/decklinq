"use client";

import Image from "next/image";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { apiFetch, clearStoredToken, normalizeQuestionType, type Card, type Deck } from "@/lib/api";

const renderStudyMetadata = (card: Card | null, showAnswer: boolean) => {
  if (!card) {
    return null;
  }

  const questionType = normalizeQuestionType(card.questionType);

  switch (questionType) {
    case "multiple-choice": {
      const choices = card.metadata?.choices ?? [];
      if (choices.length === 0) {
        return null;
      }

      return (
        <div className="mt-4 space-y-2 rounded-xl border border-slate-700 bg-slate-900 px-4 py-3 text-sm text-slate-200">
          <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Options</p>
          {choices.map((choice, index) => {
            const isCorrect = choice.isCorrect;
            const reveal = showAnswer && isCorrect;
            return (
              <div key={`${choice.label}-${index}`} className={reveal ? "rounded-lg border border-emerald-500/40 bg-emerald-500/10 px-3 py-2 text-emerald-200" : "rounded-lg border border-slate-700 bg-slate-950/40 px-3 py-2"}>
                {choice.label}
                {showAnswer && isCorrect ? " • correct" : ""}
              </div>
            );
          })}
        </div>
      );
    }
    case "true-false": {
      const correctAnswer = card.metadata?.correctAnswer ?? false;
      return (
        <div className="mt-4 rounded-xl border border-slate-700 bg-slate-900 px-4 py-3 text-sm text-slate-200">
          <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Answer</p>
          <p className="mt-2 font-medium text-cyan-200">{showAnswer ? (correctAnswer ? "True" : "False") : "Choose True or False"}</p>
        </div>
      );
    }
    case "image": {
      const imageUrl = card.metadata?.imageUrl;
      if (!showAnswer || !imageUrl) {
        return (
          <div className="mt-4 rounded-xl border border-slate-700 bg-slate-900 px-4 py-3 text-sm text-slate-300">
            {showAnswer ? "No image available." : "Image prompt coming up."}
          </div>
        );
      }

      return (
        <div className="mt-4 rounded-xl border border-slate-700 bg-slate-900 px-4 py-3 text-sm text-slate-200">
          <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Image answer</p>
          <div className="relative mt-3 h-64 w-full overflow-hidden rounded-xl border border-slate-700 bg-slate-950">
            <Image src={imageUrl} alt={card.frontContent} fill unoptimized className="object-contain" />
          </div>
        </div>
      );
    }
    case "cloze": {
      const blankWord = card.metadata?.blankWord;
      const answer = card.metadata?.answer;
      return (
        <div className="mt-4 rounded-xl border border-slate-700 bg-slate-900 px-4 py-3 text-sm text-slate-200">
          <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Cloze</p>
          <p className="mt-2">{showAnswer ? (answer ?? blankWord ?? "—") : (blankWord ? `Blank: ${blankWord}` : "Fill in the missing word")}</p>
        </div>
      );
    }
    case "basic":
    default:
      return showAnswer ? (
        <div className="mt-4 rounded-xl border border-slate-700 bg-slate-900 px-4 py-3 text-sm text-slate-200">
          {card.backContent}
        </div>
      ) : null;
  }
};

export default function StudyPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const deckId = Number(params.id);
  const [deck, setDeck] = useState<Deck | null>(null);
  const [loading, setLoading] = useState(true);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [showAnswer, setShowAnswer] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!Number.isFinite(deckId)) {
      router.replace("/dashboard");
      return;
    }

    let active = true;

    const load = async () => {
      try {
        const data = await apiFetch<Deck>(`/api/decks/${deckId}`);
        if (!active) {
          return;
        }

        setDeck(data);
        setCurrentIndex(0);
        setShowAnswer(false);
      } catch (loadError) {
        if (!active) {
          return;
        }

        if (loadError instanceof Error && loadError.message === "Unauthorized") {
          clearStoredToken();
          router.replace("/login");
          return;
        }

        setError(loadError instanceof Error ? loadError.message : "Unable to load study deck.");
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    };

    void load();

    return () => {
      active = false;
    };
  }, [deckId, router]);

  const cards = deck?.cards ?? [];
  const currentCard: Card | null = cards[currentIndex] ?? null;
  const isComplete = deck !== null && cards.length > 0 && currentIndex >= cards.length;
  const progressText = deck ? `${Math.min(currentIndex + (currentCard ? 1 : 0), cards.length)} / ${cards.length}` : "0 / 0";

  const handleNextCard = () => {
    if (!deck || cards.length === 0) {
      return;
    }

    if (currentIndex >= cards.length - 1) {
      setCurrentIndex(cards.length);
      setShowAnswer(false);
      return;
    }

    setCurrentIndex((previousIndex) => previousIndex + 1);
    setShowAnswer(false);
  };

  const handleReset = () => {
    setCurrentIndex(0);
    setShowAnswer(false);
  };

  if (loading) {
    return (
      <main className="min-h-screen bg-slate-950 px-6 py-8 text-slate-100">
        <div className="mx-auto max-w-4xl rounded-3xl border border-slate-800 bg-slate-900/80 p-8 text-slate-300">
          Loading your study deck...
        </div>
      </main>
    );
  }

  if (error) {
    return (
      <main className="min-h-screen bg-slate-950 px-6 py-8 text-slate-100">
        <div className="mx-auto max-w-4xl rounded-3xl border border-rose-500/40 bg-rose-500/10 p-8 text-rose-200">
          {error}
        </div>
      </main>
    );
  }

  if (!deck) {
    return null;
  }

  if (cards.length === 0) {
    return (
      <main className="min-h-screen bg-slate-950 px-6 py-8 text-slate-100">
        <div className="mx-auto max-w-3xl rounded-3xl border border-slate-800 bg-slate-900/80 p-8">
          <p className="text-sm text-cyan-300">{deck.title}</p>
          <h1 className="mt-2 text-3xl font-bold">This deck is empty</h1>
          <p className="mt-4 text-slate-400">Add a few cards in the editor before starting study mode.</p>
          <div className="mt-6 flex gap-3">
            <Link href={`/decks/${deck.id}`} className="rounded-xl bg-cyan-400 px-4 py-2 font-semibold text-slate-950 hover:bg-cyan-300">
              Edit deck
            </Link>
            <Link href="/dashboard" className="rounded-xl border border-slate-700 px-4 py-2 font-semibold text-slate-200 hover:bg-slate-800">
              Back to dashboard
            </Link>
          </div>
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-slate-950 px-6 py-8 text-slate-100">
      <div className="mx-auto max-w-4xl">
        <header className="mb-6 flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div>
            <Link href="/dashboard" className="text-sm text-cyan-300 hover:text-cyan-200">← Dashboard</Link>
            <h1 className="mt-2 text-3xl font-bold">{deck.title}</h1>
          </div>
          <div className="flex items-center gap-3">
            <Link href={`/decks/${deck.id}`} className="rounded-xl border border-slate-700 bg-slate-900 px-4 py-2 text-sm font-semibold text-slate-200 hover:bg-slate-800">
              Edit deck
            </Link>
            <div className="rounded-full border border-slate-700 bg-slate-900/80 px-3 py-2 text-sm text-slate-200">
              {progressText}
            </div>
          </div>
        </header>

        {isComplete ? (
          <section className="rounded-3xl border border-slate-800 bg-slate-900/80 p-8 text-center">
            <p className="text-sm uppercase tracking-[0.3em] text-cyan-300">Study session complete</p>
            <h2 className="mt-4 text-3xl font-bold">Nice work.</h2>
            <p className="mt-3 text-slate-400">You reviewed all {cards.length} cards in this deck.</p>
            <div className="mt-6 flex justify-center gap-3">
              <button
                type="button"
                onClick={handleReset}
                className="rounded-xl bg-cyan-400 px-4 py-2 font-semibold text-slate-950 hover:bg-cyan-300"
              >
                Study again
              </button>
              <Link href={`/decks/${deck.id}`} className="rounded-xl border border-slate-700 px-4 py-2 font-semibold text-slate-200 hover:bg-slate-800">
                Back to deck
              </Link>
            </div>
          </section>
        ) : (
          <section className="rounded-3xl border border-slate-800 bg-slate-900/80 p-6 md:p-8">
            <div className="mb-4 flex items-center justify-between text-sm text-slate-400">
              <span>Card {currentIndex + 1}</span>
              {currentCard?.isLaTeX ? <span className="rounded-full border border-cyan-500/40 bg-cyan-500/10 px-2 py-1 text-cyan-200">LaTeX</span> : null}
            </div>

            <div className="rounded-2xl border border-slate-700 bg-slate-950/60 p-6">
              <p className="mb-3 text-xs uppercase tracking-[0.25em] text-slate-500">Front</p>
              <p className="text-2xl font-semibold text-white">{currentCard?.frontContent}</p>
              {currentCard ? renderStudyMetadata(currentCard, false) : null}
            </div>

            <div className="mt-6 rounded-2xl border border-slate-700 bg-slate-950/60 p-6">
              <p className="mb-3 text-xs uppercase tracking-[0.25em] text-slate-500">Answer</p>
              {showAnswer ? (
                <div>{renderStudyMetadata(currentCard, true)}</div>
              ) : (
                <p className="text-slate-500">Reveal the answer when you are ready.</p>
              )}
            </div>

            {currentCard?.hints ? (
              <div className="mt-4 rounded-xl border border-slate-700 bg-slate-900 px-4 py-3 text-sm text-slate-300">
                Hint: {currentCard.hints}
              </div>
            ) : null}

            <div className="mt-6 flex flex-wrap gap-3">
              <button
                type="button"
                onClick={() => setShowAnswer((previous) => !previous)}
                className="rounded-xl bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300"
              >
                {showAnswer ? "Hide answer" : "Reveal answer"}
              </button>

              <button
                type="button"
                onClick={handleNextCard}
                className="rounded-xl border border-slate-700 bg-slate-900 px-4 py-2.5 font-semibold text-slate-200 hover:bg-slate-800"
              >
                {currentIndex >= cards.length - 1 ? "Finish study" : "Next card"}
              </button>
            </div>
          </section>
        )}
      </div>
    </main>
  );
}
