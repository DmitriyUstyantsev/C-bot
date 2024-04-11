//4)	Уровень бизнес-логики. PatientPersonalAssistant.BAL.Core.

namespace PatientPersonalAssistant.BAL.Core.Models
{
public class Answer
{
public decimal Id { get; set; }
public string TextOfAnswer { get; set; }
}
}

namespace PatientPersonalAssistant.BAL.Core.Models
{
public class BranchBAL
{
public decimal Id { get; set; } public string Name { get; set; }
}
}

namespace PatientPersonalAssistant.BAL.Core.Models
{
public class CorrelationOfAnswerAndDiagnosis
{
public decimal Id { get; set; }
public decimal WeightOfAnswer { get; set; }  public decimal AnswerToTheQuestionId { get; set; }
}
}

using System.Collections.Generic;


namespace PatientPersonalAssistant.BAL.Core.Models
{
public class DataFromKnowlegeBase
{
public decimal Id { get; set; } public string Name { get; set; }
public string Recommendation { get; set; }


public List<CorrelationOfAnswerAndDiagnosis> Correlation { get; set; }

public DataFromKnowlegeBase()
{
Correlation = new List<CorrelationOfAnswerAndDiagnosis>();
}
}
}

namespace PatientPersonalAssistant.BAL.Core.Models
{
public class ProcessedData
{
public string Name { get; set; }
public string Recommendation { get; set; } public decimal Persent { get; set; }
}
}

using System.Collections.Generic;


namespace PatientPersonalAssistant.BAL.Core.Models
{
public class QuestionWithAnswers
{
public decimal Id { get; set; }
public string QuestionText { get; set; } public decimal BranchId { get; set; } public List<Answer> Answers { get; set; }

public QuestionWithAnswers()
{
Answers = new List<Answer>();
}
}
}

namespace PatientPersonalAssistant.BAL.Core.Models
{
public class TelegramUser
{
public int TelegaramId { get; set; } public string UserState { get; set; } public decimal BranchId { get; set; }
public string CodeOfEntry { get; set; }
}
}

namespace PatientPersonalAssistant.BAL.Core.Models
{
public class UserSurveyDataBAL
{
public string CodeOfEntry { get; set; } public int TelegramId { get; set; } public string Date { get; set; }
public decimal AnswerToTheQuestionId { get; set; }
}
}