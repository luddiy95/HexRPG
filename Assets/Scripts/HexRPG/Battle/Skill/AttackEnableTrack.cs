using UnityEngine.Playables;
using UnityEngine;
using UnityEngine.Timeline;

namespace HexRPG.Battle.Skill
{
    [TrackBindingType(typeof(IAttackSkillController))]
    [TrackClipType(typeof(AttackEnablePlayableAsset))]
    public class AttackEnableTrack : TrackAsset
    {
        protected override Playable CreatePlayable(PlayableGraph graph, GameObject go, TimelineClip clip)
        {
            AttackEnableBehaviour behaviour = new AttackEnableBehaviour();

            //cubeObject�Ƃ����v���p�e�B�̏����ACreatePlayable�̈����œn�����I�u�W�F�N�g�̐ݒ�
            if(go.TryGetComponent(out ICustomComponentCollection owner))
            {
                if(owner.QueryInterface(out IAttackSkillController attackSkillController)) {
                    behaviour.attackSkillController = attackSkillController;
                }
            }

            ScriptPlayable<AttackEnableBehaviour> playable = ScriptPlayable<AttackEnableBehaviour>.Create(graph, behaviour);
            return playable;
        }
    }
}