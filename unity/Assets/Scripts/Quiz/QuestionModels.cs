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
    public string id;
    public string valence;
    public string doc_type;
    public string title;
    public int time_limit_sec;
    public string tags;
    public string body;

    public string A;
    public string B;
    public string C;
    public string D;

    public string answer;
    public string rationale;
    public string source;
}
