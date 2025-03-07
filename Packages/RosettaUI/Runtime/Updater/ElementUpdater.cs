﻿using System.Collections.Generic;
using System.Linq;

namespace RosettaUI
{
    public class ElementUpdater
    {
        private readonly HashSet<Element> _elements = new();
        private readonly Queue<Element> _registerQueue = new();
        private readonly Queue<Element> _unregisterQueue = new();

        public IReadOnlyCollection<Element> Elements => _elements;

        public void RegisterWindowRecursive(Element element)
        {
            if (element is WindowElement)
            {
                Register(element);
            }
            else if (element is WindowLauncherElement windowLauncherElement)
            {
                RegisterWindowRecursive(windowLauncherElement.window);
            }
            
            if (element is ElementGroup elementGroup)
            {
                foreach (var e in elementGroup.Children)
                {
                    RegisterWindowRecursive(e);
                }

                if (element is DynamicElement dynamicElement)
                {
                    dynamicElement.onRebuildChildren -= RegisterWindowRecursive;
                    dynamicElement.onRebuildChildren += RegisterWindowRecursive;
                }
            }
        }
        
        public void Register(Element element)
        {
            if (element == null) return;
            _registerQueue.Enqueue(element);
        }

        public void Unregister(Element element)
        {
            _unregisterQueue.Enqueue(element);
        }


        public void Update()
        {
            ProcessQueue();

            Getter.EnableCache();

            foreach (var e in _elements)
            {
                e.Update();
            }
            
            Getter.DisableCache();
        }

        /// <summary>
        /// Update()内のループ中のElement.Update()内でRegister()/Unregister()されることがあるので
        /// 次のUpdate()までキューに貯める
        /// </summary>
        void ProcessQueue()
        {
            while (_registerQueue.Any())
            {
                var e = _registerQueue.Dequeue();
                if (_elements.Add(e))
                {
                    e.onDestroy += (element,_) => Unregister(element);
                }
            }


            while (_unregisterQueue.Any())
            {
                var e = _unregisterQueue.Dequeue();
                _elements.Remove(e);
            }
        }
    }
}