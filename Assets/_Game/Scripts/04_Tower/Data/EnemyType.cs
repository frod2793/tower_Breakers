namespace TowerBreakers.Tower.Data
{
    /// <summary>
    /// [기능]: 적 타입 열거형
    /// </summary>
    public enum EnemyType
    {
        /// <summary>
        /// 일반 몹 - 기차 행렬 형태로 이동
        /// </summary>
        Normal = 0,

        /// <summary>
        /// 엘리트 몹 - 일반 몹과 함께 스폰되지만 독자적으로 움직임
        /// </summary>
        Elite = 1,

        /// <summary>
        /// 보스 - 다른 몹들과 함께 스폰되지 않음
        /// </summary>
        Boss = 2
    }
}
