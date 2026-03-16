using TowerBreakers.Player.Data;
using VContainer;

namespace TowerBreakers.Core.SceneManagement
{
    /// <summary>
    /// [설명]: 씬 전환 시 전달되는 컨텍스트 데이터를 담는 DTO 클래스입니다.
    /// </summary>
    public class SceneContextDTO
    {
        public EquipmentDTO Equipment { get; set; }

        /// <summary>
        /// [설명]: 씬 전환 시 추가적으로 전달할 데이터들을 담는 저장소입니다.
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> ExtraData { get; private set; }

        [Inject]
        public SceneContextDTO()
        {
            Equipment = new EquipmentDTO();
            ExtraData = new System.Collections.Generic.Dictionary<string, object>();
        }

        public SceneContextDTO(EquipmentDTO equipment)
        {
            Equipment = equipment;
            ExtraData = new System.Collections.Generic.Dictionary<string, object>();
        }
    }
}
