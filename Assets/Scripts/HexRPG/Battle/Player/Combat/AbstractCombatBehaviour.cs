using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using System;
using UniRx;

namespace HexRPG.Battle.Player.Combat
{
    public class AbstractCombatBehaviour : MonoBehaviour, ICombat, ICombatObservable
    {
        [SerializeField] protected PlayableDirector _director;

        IObservable<Unit> ICombatObservable.OnFinishCombat => _onFinishCombat;
        readonly ISubject<Unit> _onFinishCombat = new Subject<Unit>();

        ICharacterComponentCollection _combatOrigin;

        void ICombat.Init(PlayableAsset timeline, ICharacterComponentCollection combatOrigin, Animator animator)
        {
            //_skillEffect.SetActive(false);
            _combatOrigin = combatOrigin;

            _director.playableAsset = timeline;

            foreach (var bind in _director.playableAsset.outputs)
            {
                if (bind.streamName == "Animation Track")
                {
                    _director.SetGenericBinding(bind.sourceObject, animator);
                }
            }

            _director.stopped += ((obj) => _onFinishCombat.OnNext(Unit.Default));
        }

        void ICombat.Execute()
        {
            //TODO: 通常攻撃開始/コンボ
        }
    }
}
