using TowerBreakers.Player.Data;

namespace TowerBreakers.Core.SceneManagement
{
    /// <summary>
    /// [설명]: 씬 전환 시 전달되는 컨텍스트 데이터를 담는 DTO 클래스입니다.
    /// </summary>
    public class SceneContextDTO
    {
        public EquipmentDTO Equipment { get; set; }

        public SceneContextDTO()
        {
            Equipment = new EquipmentDTO();
        }

        public SceneContextDTO(EquipmentDTO equipment)
        {
            Equipment = equipment;
        }
    }
}
