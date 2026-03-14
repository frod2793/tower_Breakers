namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적 캐릭터에게 적용할 넉백의 연출 타입을 정의합니다.
    /// </summary>
    public enum KnockbackType
    {
        /// <summary> [설명]: 넉백 없음 </summary>
        None,
        /// <summary> [설명]: 특정 좌표로 직접 이동 (DOMove) </summary>
        Translate,
        /// <summary> [설명]: 제자리에서 튕기는 연출 (DOPunchPosition) </summary>
        Punch
    }
}
