using System;
using System.Collections;
using System.Collections.Generic;
using StansAssets.Foundation.Async;
using UnityEngine;

namespace StansAssets.SceneManagement
{
    public class SceneActionsQueue
    {
        IScenePreloader m_Preloader;
        AsyncOperation m_CurrentAsyncOperation;
        bool m_IsRunning;
        readonly ISceneLoadService m_SceneLoadService;
        readonly Stack<SceneAction> m_ActionsStack = new Stack<SceneAction>();
        readonly List<ISceneManager> m_SceneManagers = new List<ISceneManager>();


        public IEnumerable<SceneAction> ScheduledActions => m_ActionsStack;

        public SceneActionsQueue(ISceneLoadService sceneLoadService)
        {
            m_SceneLoadService = sceneLoadService;
        }

        public void SetPreloader(IScenePreloader preloader)
        {
            m_Preloader = preloader;
        }

        public void AddAction(SceneActionType type, string sceneName)
        {
            var data = new SceneAction
            {
                Type = type,
                SceneName = sceneName
            };

            m_ActionsStack.Push(data);
        }


        public void Start(Action onComplete = null)
        {
            m_SceneManagers.Clear();
            if (m_Preloader != null)
            {
                m_Preloader.FadeIn(() =>
                {
                    StartActionsStack(() =>
                    {
                        m_Preloader.FadeOut(() =>
                        {
                            onComplete?.Invoke();
                        });
                    });
                });
            }
            else
            {
                StartActionsStack(onComplete);
            }
        }

        public T GetLoadedSceneManager<T>() where T : ISceneManager
        {
            foreach (var sceneManager in m_SceneManagers)
            {
                if (sceneManager.GetType() == typeof(T))
                    return (T)sceneManager;
            }

            return default;
        }

        public IEnumerator OnStackProgress()
        {
            while (m_IsRunning)
            {
                if (m_CurrentAsyncOperation != null)
                {
                    m_Preloader?.OnProgress(m_CurrentAsyncOperation.progress);
                }

                yield return new WaitForEndOfFrame();
            }
        }

        void StartActionsStack(Action onComplete)
        {
            m_IsRunning = true;
            CoroutineUtility.Start(OnStackProgress());
            ExecuteActionsStack(onComplete);
        }

        void ExecuteActionsStack(Action onComplete)
        {
            if (m_ActionsStack.Count == 0)
            {
                m_IsRunning = false;
                CoroutineUtility.Stop(OnStackProgress());
                onComplete?.Invoke();
                return;
            }

            var actionData = m_ActionsStack.Pop();
            switch (actionData.Type)
            {
                case SceneActionType.Load:
                    m_SceneLoadService.Load(actionData.SceneName, sceneManager =>
                    {
                        if(sceneManager != null)
                            m_SceneManagers.Add(sceneManager);

                        ExecuteActionsStack(onComplete);
                    });

                    m_CurrentAsyncOperation = AdditiveScenesLoader.GetSceneAsyncOperation(actionData.SceneName);
                    break;
                case SceneActionType.Deactivate:
                    m_SceneLoadService.Deactivate(actionData.SceneName, () =>
                    {
                        ExecuteActionsStack(onComplete);
                    });
                    break;
                case SceneActionType.Unload:
                    m_SceneLoadService.Unload(actionData.SceneName, () =>
                    {
                        ExecuteActionsStack(onComplete);
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }
}