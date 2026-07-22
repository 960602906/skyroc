namespace Application.AI.Capabilities;

/// <summary>生成并校验 ControllerName.ActionName 格式的稳定 AI 操作标识。</summary>
public static class AiOperationId
{
    /// <summary>按控制器类型名和 Action 名生成稳定操作标识。</summary>
    public static string Create(string controllerName, string actionName)
    {
        if (string.IsNullOrWhiteSpace(controllerName))
            throw new ArgumentException("控制器名称不能为空。", nameof(controllerName));
        if (string.IsNullOrWhiteSpace(actionName))
            throw new ArgumentException("Action 名称不能为空。", nameof(actionName));

        return $"{controllerName.Trim()}.{actionName.Trim()}";
    }

    /// <summary>拒绝能力目录中大小写不敏感的重复操作标识。</summary>
    public static void EnsureUnique(IEnumerable<string> operationIds)
    {
        ArgumentNullException.ThrowIfNull(operationIds);
        var duplicate = operationIds
            .GroupBy(operationId => operationId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"AI operationId '{duplicate.Key}' 重复，能力目录无法启动。");
    }
}
