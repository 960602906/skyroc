using Application.Exceptions;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     T13 故障注入门闩：按剩余次数在关键业务同步点抛出业务异常，用于验证跨模块事务回滚。
/// </summary>
public sealed class FaultInjectionGate
{
    private int _customerBillAcceptanceFailuresRemaining;

    /// <summary>
    ///     武装客户账单签收同步故障；下一次（或指定次数）同步将抛出业务异常。
    /// </summary>
    public void ArmCustomerBillAcceptanceSync(int times = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(times, 1);
        Interlocked.Exchange(ref _customerBillAcceptanceFailuresRemaining, times);
    }

    /// <summary>
    ///     解除全部故障注入。
    /// </summary>
    public void Disarm()
    {
        Interlocked.Exchange(ref _customerBillAcceptanceFailuresRemaining, 0);
    }

    /// <summary>
    ///     若仍有剩余签收账单同步故障次数，则扣减一次并抛出业务异常。
    /// </summary>
    public void ThrowIfCustomerBillAcceptanceArmed()
    {
        while (true)
        {
            var current = Volatile.Read(ref _customerBillAcceptanceFailuresRemaining);
            if (current <= 0)
                return;

            if (Interlocked.CompareExchange(
                    ref _customerBillAcceptanceFailuresRemaining,
                    current - 1,
                    current) == current)
            {
                throw new BusinessException("T13故障注入：客户账单同步故意失败，用于验证签收事务回滚");
            }
        }
    }
}
