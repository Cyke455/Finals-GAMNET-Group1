using UnityEngine;

    public class FlagController : MonoBehaviour
    {
        private Animator _animator;
        public bool IsActivated { get; private set; }

        private void Awake()
        {
            _animator=GetComponent<Animator>();
        }

        public void RaiseFlag()
        {
            _animator.SetTrigger("Out");
            IsActivated = true;
        }
    }