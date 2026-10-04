-- Step 1: Create the new enum type QuestionType with the desired values
CREATE TYPE QuestionType AS ENUM ('basic', 'true-false', 'multiple-choice', 'cloze', 'image');

-- Step 2: Alter the Cards table to add the new QuestionType column with the enum type
ALTER TABLE "Cards"
ALTER COLUMN "QuestionType" TYPE QuestionType
USING "QuestionType"::QuestionType;
