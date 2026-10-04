-- Add QuestionType and Metadata columns to Cards for new quiz/question types
ALTER TABLE "Cards"
    ADD COLUMN IF NOT EXISTS "QuestionType" VARCHAR(100);

ALTER TABLE "Cards"
    ADD COLUMN IF NOT EXISTS "Metadata" TEXT;

-- Optionally populate existing rows with default question type
UPDATE "Cards" SET "QuestionType" = 'basic' WHERE "QuestionType" IS NULL;
