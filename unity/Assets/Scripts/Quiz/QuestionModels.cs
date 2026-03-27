using System;
using System.Collections.Generic;

[Serializable]
public class QuestionList
{
    public List<Question> questions;
}

[Serializable]
public class Question
{
    // -----------------------------
    // Ana metadata
    // -----------------------------
    public string id;
    public string valence;
    public string doc_type;
    public string title;
    public string tags;
    public string source;

    public string topic;
    public string region;
    public string concept_type;

    // -----------------------------
    // Ek alanlar
    // -----------------------------
    public string level;
    public string hint;
    public string rationale;

    // -----------------------------
    // Süre
    // -----------------------------
    public int time_limit_sec;

    // -----------------------------
    // Eski basic concept yapısı
    // -----------------------------
    public string body;

    public string A;
    public string B;
    public string C;
    public string D;

    // Eski datasetler için düz cevap alanı
    public string answer_text;

    // Basic excel alanları
    public string question_text;
    public string option_a;
    public string option_b;
    public string option_c;
    public string option_d;
    public string correct_answer;

    // -----------------------------
    // Yeni motion dataset yapısı
    // -----------------------------
    public QuestionData data;
    public QuestionAnswer answer;

    public string GetQuestionText()
    {
        if (!string.IsNullOrWhiteSpace(question_text))
            return question_text;

        if (!string.IsNullOrWhiteSpace(body))
            return body;

        return string.Empty;
    }

    public List<string> GetOptions()
    {
        if (data != null && data.options != null && data.options.Count > 0)
            return data.options;

        List<string> options = new List<string>();

        if (!string.IsNullOrWhiteSpace(option_a)) options.Add(option_a);
        if (!string.IsNullOrWhiteSpace(option_b)) options.Add(option_b);
        if (!string.IsNullOrWhiteSpace(option_c)) options.Add(option_c);
        if (!string.IsNullOrWhiteSpace(option_d)) options.Add(option_d);

        if (options.Count > 0)
            return options;

        if (!string.IsNullOrWhiteSpace(A)) options.Add(A);
        if (!string.IsNullOrWhiteSpace(B)) options.Add(B);
        if (!string.IsNullOrWhiteSpace(C)) options.Add(C);
        if (!string.IsNullOrWhiteSpace(D)) options.Add(D);

        return options;
    }

    public int GetCorrectIndex()
    {
        if (answer != null)
            return answer.correct_index;

        if (!string.IsNullOrWhiteSpace(correct_answer))
            return LetterToIndex(correct_answer);

        if (!string.IsNullOrWhiteSpace(answer_text))
            return LetterToIndex(answer_text);

        return -1;
    }

    public List<string> GetMatchingLeft()
    {
        if (data != null && data.left != null)
            return data.left;

        return new List<string>();
    }

    public List<string> GetMatchingRight()
    {
        if (data != null && data.right != null)
            return data.right;

        return new List<string>();
    }

    public List<MatchPair> GetMatchingPairs()
    {
        if (answer != null && answer.pairs != null)
            return answer.pairs;

        return new List<MatchPair>();
    }

    public int GetMatchingRightIndexForLeft(int leftIndex)
    {
        List<MatchPair> pairs = GetMatchingPairs();

        for (int i = 0; i < pairs.Count; i++)
        {
            if (pairs[i].leftIndex == leftIndex)
                return pairs[i].rightIndex;
        }

        return -1;
    }

    public bool ShouldShuffleMatchingRight()
    {
        return data != null && data.shuffle_right;
    }

    private int LetterToIndex(string letter)
    {
        switch (letter.Trim().ToUpper())
        {
            case "A": return 0;
            case "B": return 1;
            case "C": return 2;
            case "D": return 3;
            default: return -1;
        }
    }

    public bool IsMatching()
    {
        return !string.IsNullOrWhiteSpace(doc_type) && doc_type.Trim().ToLower() == "matching";
    }

    public bool IsChoiceQuestion()
    {
        if (string.IsNullOrWhiteSpace(doc_type))
            return true;

        string type = doc_type.Trim().ToLower();

        return type == "mcq_single" || type == "true_false";
    }

    public int GetTimeLimit()
    {
        return time_limit_sec;
    }

    public string GetNormalizedLevel()
    {
        if (string.IsNullOrWhiteSpace(level))
            return "medium";

        string normalized = level.Trim().ToLower();

        switch (normalized)
        {
            case "easy":
            case "medium":
            case "hard":
                return normalized;
            default:
                return "medium";
        }
    }

    public string GetNormalizedRegion()
    {
        if (string.IsNullOrWhiteSpace(region))
            return string.Empty;

        return region.Trim();
    }

    public bool HasValidLevel()
    {
        string normalized = GetNormalizedLevel();
        return normalized == "easy" || normalized == "medium" || normalized == "hard";
    }

    public bool IsLevel(string targetLevel)
    {
        if (string.IsNullOrWhiteSpace(targetLevel))
            return false;

        return GetNormalizedLevel() == targetLevel.Trim().ToLower();
    }

}

[Serializable]
public class QuestionData
{
    public List<string> options;

    public List<string> left;
    public List<string> right;
    public bool shuffle_right;
}

[Serializable]
public class QuestionAnswer
{
    public int correct_index;
    public List<MatchPair> pairs;
}

[Serializable]
public class MatchPair
{
    public int leftIndex;
    public int rightIndex;
}