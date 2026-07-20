using System.Globalization;
using System.Text.Json;

namespace dxpmt.Services;

public sealed class BaselineContentFormatter
{
    private static readonly ISet<string> TechnicalProperties = new HashSet<string>
    {
        "Id", "CaseId", "WorkItemId", "ProblemId", "IsDeleted", "DeletedAt", "CreatedAt", "UpdatedAt"
    };

    private static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        ["ProblemToSolve"] = "解決したい問題", ["OccurrenceSituation"] = "発生状況", ["TargetBusinessAndDepartments"] = "対象業務・部署",
        ["CurrentResponse"] = "現在の対応方法", ["ExpectedState"] = "期待する状態", ["PreferredApproach"] = "希望する手段（参考）",
        ["Impact"] = "影響", ["Frequency"] = "発生頻度", ["NumberOfCases"] = "件数", ["OccurrencePeriod"] = "発生時期",
        ["ExamplesAndEvidence"] = "具体例・根拠", ["RelatedCasesAndExistingFunctions"] = "関連案件・既存機能", ["ScopeSummary"] = "対象範囲の概要",
        ["RelatedDepartments"] = "関係部署", ["InitialDecision"] = "初期判断", ["DecisionReason"] = "判断理由", ["NextPerson"] = "次の担当者", ["NextDueDate"] = "次回期限",
        ["ManHoursAndCost"] = "工数・コスト", ["DeliveryAndCapacity"] = "納期・能力", ["Quality"] = "品質", ["InventoryAndLogistics"] = "在庫・物流",
        ["SafetyAndCompliance"] = "安全・法令順守", ["InternalControlAndSecurity"] = "内部統制・セキュリティ", ["CustomersAndPartners"] = "顧客・取引先",
        ["SurveyLead"] = "調査責任者", ["SurveyStartDate"] = "調査開始日", ["SurveyEndDate"] = "調査終了日", ["TargetSitesAndDepartments"] = "対象拠点・部署",
        ["StartEvent"] = "開始イベント", ["EndStateAndDeliverable"] = "終了状態・成果物", ["TargetBusiness"] = "対象業務", ["TargetProductCustomerPeriod"] = "対象製品・顧客・期間",
        ["ExcludedScope"] = "対象外範囲", ["ExceptionConditions"] = "例外条件", ["Stakeholders"] = "関係者", ["SurveySteps"] = "調査ステップ", ["RequiredMaterials"] = "必要資料",
        ["DepartmentOrOrganization"] = "部署・組織", ["NameAndRole"] = "氏名・役割", ["ConfirmationTarget"] = "確認対象", ["ParticipationMethod"] = "参加方法",
        ["Sequence"] = "番号", ["Method"] = "方法", ["Target"] = "対象", ["Owner"] = "担当者", ["PlannedOn"] = "予定日", ["IsCompleted"] = "完了",
        ["Name"] = "名称", ["OwningDepartment"] = "所管部署", ["RequestedOn"] = "依頼日", ["ReceivedOn"] = "受領日", ["Notes"] = "備考",
        ["FlowName"] = "フロー名", ["Scope"] = "対象範囲", ["Author"] = "作成者", ["Reviewer"] = "確認者", ["KeyJudgements"] = "主な判断", ["KeyExceptions"] = "主な例外",
        ["IsConnectedEndToEnd"] = "開始から終了まで接続済み", ["AreDepartmentsClear"] = "部署・役割が明確", ["AreInputsOutputsConnected"] = "入出力が接続済み",
        ["AreJudgementCriteriaConfirmed"] = "判断基準を確認済み", ["AreExceptionsIncluded"] = "例外を含む", ["IsSeparatedFromToBe"] = "To-Beと区分済み",
        ["BusinessProcessName"] = "業務・工程名", ["WorkType"] = "作業区分", ["DepartmentAndRole"] = "担当部署・役割", ["Performer"] = "実施者", ["Location"] = "実施場所",
        ["IsConfirmed"] = "内容確認済み", ["ContentJson"] = "作業内容", ["Category"] = "分類", ["Phenomenon"] = "現象", ["OccurrenceCondition"] = "発生条件",
        ["CurrentWorkaround"] = "現在の対処", ["CauseHypothesis"] = "原因仮説", ["Evidence"] = "根拠", ["Severity"] = "重要度", ["Status"] = "状態",
        ["Title"] = "件名", ["Approach"] = "改善アプローチ", ["ExpectedEffect"] = "期待効果", ["CostAndEffort"] = "コスト・工数", ["RisksAndConstraints"] = "リスク・制約", ["Recommendation"] = "推奨度",
        ["Description"] = "内容", ["Priority"] = "優先度", ["AcceptanceCriteria"] = "受入条件", ["ImplementationApproach"] = "実現方法",
        ["MeasurementStartOn"] = "測定開始日", ["MeasurementEndOn"] = "測定終了日", ["Baseline"] = "改善前の基準値", ["TargetEffect"] = "目標効果",
        ["ActualEffect"] = "実績効果", ["MeasurementMethod"] = "測定方法・根拠", ["DifferenceAnalysis"] = "差異の分析", ["RemainingIssues"] = "残課題",
        ["StandardizationPlan"] = "標準化・横展開", ["FollowUpPlan"] = "フォローアップ計画"
    };

    public IReadOnlyList<BaselineContentItem> Format(string contentJson)
    {
        if (string.IsNullOrWhiteSpace(contentJson)) return [];

        try
        {
            using var document = JsonDocument.Parse(contentJson);
            var items = new List<BaselineContentItem>();
            Append(document.RootElement, "", "", items);
            return items.Count > 0 ? items : [];
        }
        catch (JsonException)
        {
            return [new BaselineContentItem("", "内容", "内容を表示できません")];
        }
    }

    private static void Append(JsonElement element, string section, string label, ICollection<BaselineContentItem> items)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (TechnicalProperties.Contains(property.Name)) continue;
                    var propertyLabel = GetLabel(property.Name);
                    if (property.Name == "ContentJson" && property.Value.ValueKind == JsonValueKind.String && TryAppendEmbeddedJson(property.Value.GetString(), section, propertyLabel, items))
                        continue;
                    if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                        Append(property.Value, string.IsNullOrEmpty(label) ? propertyLabel : label, "", items);
                    else
                        Append(property.Value, section, propertyLabel, items);
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var arrayItem in element.EnumerateArray())
                {
                    index++;
                    Append(arrayItem, string.IsNullOrEmpty(section) ? $"{label} {index}" : $"{section} {index}", "", items);
                }
                break;
            case JsonValueKind.Null:
                break;
            default:
                var value = FormatValue(element);
                if (!string.IsNullOrWhiteSpace(value))
                    items.Add(new BaselineContentItem(section, string.IsNullOrEmpty(label) ? "内容" : label, value));
                break;
        }
    }

    private static string GetLabel(string propertyName) => Labels.TryGetValue(propertyName, out var label) ? label : propertyName;

    private static bool TryAppendEmbeddedJson(string? content, string section, string label, ICollection<BaselineContentItem> items)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;
        try
        {
            using var document = JsonDocument.Parse(content);
            Append(document.RootElement, string.IsNullOrEmpty(section) ? label : $"{section} / {label}", "", items);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string FormatValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.True => "はい",
        JsonValueKind.False => "いいえ",
        JsonValueKind.String => FormatString(element.GetString() ?? string.Empty),
        _ => element.ToString()
    };

    private static string FormatString(string value)
    {
        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return date.ToString("yyyy/MM/dd");
        return value;
    }
}

public sealed record BaselineContentItem(string Section, string Label, string Value);
