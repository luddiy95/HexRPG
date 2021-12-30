using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle.Manager
{
    public class BaseManager : MonoBehaviour
    {
        public TaskManager taskManager { get; private set; }

        private void Start()
        {
            Init();
        }

        protected virtual void Init()
        {
            taskManager = new TaskManager();
        }

        void Update()
        {
            GameUpdate();
        }

        public virtual void GameUpdate()
        {
            taskManager.Update();
        }
    }

    /// <summary>
    /// �e�N���X�Ŕ��������^�X�N���Ǘ����Ď��s����N���X
    /// ��{�I�ɃR���[�`���̑�p�Ƃ��Ďg�p
    /// </summary>
    public class TaskManager
    {
        public class Task
        {
            // �^�X�N�̎��s�^�C�v
            public enum Type
            {
                None,
                Flame,      // �w��t���[�����߂�������s
                Time,       // �w�莞�Ԃ��߂�������s
                LoopFlame,  // �w��t���[�����߂���܂Ŏ��s
                LoopTime,   // �w�莞�Ԃ��߂���܂Ŏ��s
            }

            // �e�N���X����o�^�������s�֐�
            public Action Action;

            Type type;      // ���s�^�C�v 
            int runFlame;   // ���s�t���[����
            int coutFlame;  // �t���[���J�E���g
            float runTime;  // ���s����
            float coutTime; // ���ԃJ�E���g

            public bool IsAction;   // ���s���邩����
            public bool IsRemove;   // �^�X�N��j�����邩����


            public Task(float time, Action action, Type type = Type.Time)
            {
                this.Action = action;
                this.runFlame = 0;
                this.runTime = time;
                this.type = type;
                DataInit();
            }

            public Task(int flame, Action action, Type type = Type.Flame)
            {
                this.Action = action;
                this.runFlame = flame;
                this.runTime = 0f;
                this.type = type;
                DataInit();
            }

            void DataInit()
            {
                this.IsAction = false;
                this.IsRemove = false;
                this.coutFlame = 0;
                this.coutTime = 0;
            }

            // Task�̎��s�A�j�����X�V����
            public void Update(int addFlame, float addTime)
            {
                switch (type)
                {
                    // �w��t���[�����߂�������s
                    case Type.Flame:
                        this.coutFlame += addFlame;
                        if (this.coutFlame >= this.runFlame)
                        {
                            this.IsAction = true;
                            this.IsRemove = true;
                        }
                        break;
                    // �w�莞�Ԃ��߂�������s
                    case Type.Time:
                        this.coutTime += addTime;
                        if (this.coutTime >= this.runTime)
                        {
                            this.IsAction = true;
                            this.IsRemove = true;
                        }
                        break;
                    // �w��t���[�����߂���܂Ŏ��s��������
                    case Type.LoopFlame:
                        this.IsAction = true;
                        this.coutFlame += addFlame;
                        if (this.coutFlame >= this.runFlame)
                        {
                            this.IsRemove = true;
                        }
                        break;
                    // �w�莞�Ԃ��߂���܂Ŏ��s��������
                    case Type.LoopTime:
                        this.IsAction = true;
                        this.coutTime += addTime;
                        if (this.coutTime >= this.runTime)
                        {
                            this.IsRemove = true;
                        }
                        break;
                }

                // ���s�t���O�������Ă���Ύ��s����
                if (IsAction)
                {
                    Action();
                }
            }
        }

        List<Task> taskList;    // �^�X�N���X�g

        // �Q�[���J�n������̎���
        float gameTime;
        public float GameTime { get { return gameTime; } }

        // �R���X�g���N�^
        public TaskManager()
        {
            gameTime = 0;
            taskList = new List<Task>();
        }

        public void Update()
        {
            // �Q�[�����Ԃ��v��
            gameTime += Time.unscaledDeltaTime;

            // �^�X�N���X�g�̃^�X�N���X�V���Ď��s�A�j������
            for (int i = taskList.Count - 1; i >= 0; i--)
            {
                var task = taskList[i];
                task.Update(1, Time.unscaledDeltaTime);
                if (task.IsRemove)
                {
                    taskList.RemoveAt(i);
                }
            }
        }

        // �^�X�N���X�g�Ƀ^�X�N��ǉ�
        public void Add(Task task)
        {
            taskList.Add(task);
        }

        // �^�X�N���X�g�̃^�X�N�����ׂĔj������
        public void Clear()
        {
            taskList.Clear();
        }
    }
}
