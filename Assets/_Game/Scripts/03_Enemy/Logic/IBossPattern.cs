using Cysharp.Threading.Tasks;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 보스의 개별 공격 패턴을 정의하는 인터페이스입니다.
    /// </summary>
    public interface IBossPattern
    {
        string PatternName { get; }
        UniTask ExecuteAsync(EnemyController controller, System.Threading.CancellationToken ct);
    }
}
