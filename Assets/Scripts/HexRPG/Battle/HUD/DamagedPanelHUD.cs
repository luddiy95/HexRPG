using System.Linq;
using UnityEngine;
using UnityEditor;
using UniRx;
using Zenject;
using DG.Tweening;

namespace HexRPG.Battle.HUD
{
    using Player;
    using Enemy;

    public class DamagedPanelHUD : MonoBehaviour, ICharacterHUD
    {
        BattleData _battleData;
        DisplayDataContainer _displayDataContainer;
        ITrackingHUD _trackingHUD;

        RectTransform _transform;
        [SerializeField] ObjectPool _objectPool;
        //! DamagedDisplay表示時、CharacterのDamagedモーションなどを追ってしまわないようにDamagedDisplayのParent(DamagedPanelHUD)とTrackingHUDは別にする
        [SerializeField] GameObject _trackingPanel;

        CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            BattleData battleData,
            DisplayDataContainer displayDataContainer
        )
        {
            _battleData = battleData;
            _displayDataContainer = displayDataContainer;
        }

        void Awake()
        {
            _transform = GetComponent<RectTransform>();
            _trackingHUD = _trackingPanel.GetComponent<ITrackingHUD>();
        }

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            _disposables.Clear();

            if (chara is IPlayerComponentCollection playerOwner)
            {
                playerOwner.MemberObservable.CurMember
                    .Subscribe(memberOwner =>
                    {
                        _name = memberOwner.ProfileSetting.Name;
                        UpdateDisplay();
                    })
                    .AddTo(_disposables);
            }
            if (chara is IEnemyComponentCollection enemyOwner)
            {
                _name = enemyOwner.ProfileSetting.Name;
                UpdateDisplay();
            }

            if(chara is IAttackComponentCollection owner)
            {
                owner.DamageApplicable.OnHit
                    .Subscribe(hitData =>
                    {
                        _transform.anchoredPosition = _trackingHUD.AnchoredPos;

                        var damagedDisplay = _objectPool.Instantiate().GetComponent<DamagedDisplay>();
                        damagedDisplay.AnchoredPos = new Vector2(_size.x * Random.value, _size.y * Random.value);
                        damagedDisplay.Damage = hitData.Damage;
                        if (_battleData.damagedDisplayMatMap.Table.TryGetValue(hitData.HitType, out Material mat)) damagedDisplay.Material = mat;

                        DOTween.Sequence()
                            .Append(TransformUtility.DOAnchorPosY(damagedDisplay.RectTransform, -18f, 0.3f).SetRelative(true).SetEase(Ease.OutBounce, 10))
                            .AppendInterval(0.3f)
                            .AppendCallback(() => _objectPool.Free(damagedDisplay.gameObject));

                    })
                    .AddTo(_disposables);
            }
        }

        public void UpdateDisplay()
        {
            var data = _displayDataContainer.displayDataMap.FirstOrDefault(data => data.name == _name);
            if (data == null) return;
            _trackingHUD.Offset = data.damagedPanelOffset;
            _size = data.damagedPanelSize;
            _transform.sizeDelta = _size;
        }

        void OnDestroy()
        {
            _disposables.Dispose();
        }

#if UNITY_EDITOR

        string _name;

        Vector2 _size;

        [CustomEditor(typeof(DamagedPanelHUD))]
        public class DamagedPanelHUDInspector : Editor
        {
            private void OnEnable()
            {
            }

            private void OnDisable()
            {
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                if (GUILayout.Button("UpdateData")) ((DamagedPanelHUD)target).UpdateDisplay();
            }
        }

#endif
    }
}
