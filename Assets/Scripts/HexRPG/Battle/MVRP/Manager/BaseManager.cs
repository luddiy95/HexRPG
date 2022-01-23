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
    /// 各クラスで発生したタスクを管理して実行するクラス
    /// 基本的にコルーチンの代用として使用
    /// </summary>
    public class TaskManager
    {
        public class Task
        {
            // タスクの実行タイプ
            public enum Type
            {
                None,
                Flame,      // 指定フレームが過ぎたら実行
                Time,       // 指定時間が過ぎたら実行
                LoopFlame,  // 指定フレームが過ぎるまで実行
                LoopTime,   // 指定時間が過ぎるまで実行
            }

            // 各クラスから登録される実行関数
            public Action Action;

            Type type;      // 実行タイプ 
            int runFlame;   // 実行フレーム数
            int coutFlame;  // フレームカウント
            float runTime;  // 実行時間
            float coutTime; // 時間カウント

            public bool IsAction;   // 実行するか判定
            public bool IsRemove;   // タスクを破棄するか判定


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

            // Taskの実行、破棄を更新する
            public void Update(int addFlame, float addTime)
            {
                switch (type)
                {
                    // 指定フレームが過ぎたら実行
                    case Type.Flame:
                        this.coutFlame += addFlame;
                        if (this.coutFlame >= this.runFlame)
                        {
                            this.IsAction = true;
                            this.IsRemove = true;
                        }
                        break;
                    // 指定時間が過ぎたら実行
                    case Type.Time:
                        this.coutTime += addTime;
                        if (this.coutTime >= this.runTime)
                        {
                            this.IsAction = true;
                            this.IsRemove = true;
                        }
                        break;
                    // 指定フレームが過ぎるまで実行し続ける
                    case Type.LoopFlame:
                        this.IsAction = true;
                        this.coutFlame += addFlame;
                        if (this.coutFlame >= this.runFlame)
                        {
                            this.IsRemove = true;
                        }
                        break;
                    // 指定時間が過ぎるまで実行し続ける
                    case Type.LoopTime:
                        this.IsAction = true;
                        this.coutTime += addTime;
                        if (this.coutTime >= this.runTime)
                        {
                            this.IsRemove = true;
                        }
                        break;
                }

                // 実行フラグが立っていれば実行する
                if (IsAction)
                {
                    Action();
                }
            }
        }

        List<Task> taskList;    // タスクリスト

        // ゲーム開始時からの時間
        float gameTime;
        public float GameTime { get { return gameTime; } }

        // コンストラクタ
        public TaskManager()
        {
            gameTime = 0;
            taskList = new List<Task>();
        }

        public void Update()
        {
            // ゲーム時間を計測
            gameTime += Time.unscaledDeltaTime;

            // タスクリストのタスクを更新して実行、破棄する
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

        // タスクリストにタスクを追加
        public void Add(Task task)
        {
            taskList.Add(task);
        }

        // タスクリストのタスクをすべて破棄する
        public void Clear()
        {
            taskList.Clear();
        }
    }
}
