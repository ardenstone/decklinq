-- Convert legacy metadata text to JSONB and normalize question types
ALTER TABLE "Cards"
    ALTER COLUMN "Metadata" TYPE JSONB
    USING CASE
        WHEN "Metadata" IS NULL OR btrim("Metadata") = '' THEN NULL
        ELSE "Metadata"::jsonb
    END;

-- Ensure any legacy values without a question type are set to basic
UPDATE "Cards"
SET "QuestionType" = 'basic'
WHERE "QuestionType" IS NULL;
