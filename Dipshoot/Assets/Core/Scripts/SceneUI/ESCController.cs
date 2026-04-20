using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Scripts.UI.SceneUI
{
    public class ESCController : MonoBehaviour
    {
        private Stack<ESCRequest> _requests = new();
        private HashSet<int> _blocked_requests = new();
        private int _next_id = 0;

        private void Awake()
        {
            //InputManager.Instance.GetControls().UI.Cancel.started += ProcessESCButtonClicked;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            //InputManager.Instance.GetControls().UI.Cancel.started -= ProcessESCButtonClicked;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void SetDefault()
        {
            _requests.Clear();
            _blocked_requests.Clear();
        }

        public ESCRequest AddSingleInteraction(Action func) => AddRequest(func, ESCInterationType.SINGLE);
        public ESCRequest AddMultipleInteraction(Action func) => AddRequest(func, ESCInterationType.MULTIPLE);

        public void BlockRequest(ESCRequest request)
        {
            if (request != null)
                BlockRequest(request.Id);
        }
        public void BlockRequest(int id) => _blocked_requests.Add(id);

        private ESCRequest AddRequest(Action func, ESCInterationType type) => AddRequest(new(++_next_id, func, type));
        private ESCRequest AddRequest(ESCRequest request)
        {
            _requests.Push(request);
            return request;
        }

        private void ProcessESCButtonClicked(InputAction.CallbackContext _)
        {
            if (_requests.Count == 0)
                return;

            ESCRequest request = GetPeekRequest();
            if (request == null) return;

            request.Function?.Invoke();
        }

        private ESCRequest GetPeekRequest()
        {
            while (_requests.Count > 0)
            {
                ESCRequest request = _requests.Pop();
                if (_blocked_requests.Contains(request.Id))
                    continue;

                if (request.Type == ESCInterationType.MULTIPLE)
                    _requests.Push(request);
                return request;
            }
            return null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { }// =>InputManager.Instance.GetControls().UI.Cancel.started += ProcessESCButtonClicked;
    }

    public class ESCRequest
    {
        public int Id;
        public Action Function;
        public ESCInterationType Type;

        public ESCRequest(int id, Action function, ESCInterationType type)
        {
            Id = id;
            Function = function;
            Type = type;
        }
    }

    public enum ESCInterationType
    {
        SINGLE,
        MULTIPLE
    }
}