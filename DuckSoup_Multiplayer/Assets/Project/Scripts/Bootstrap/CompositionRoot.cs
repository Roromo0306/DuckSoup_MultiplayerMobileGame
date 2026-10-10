using DuckSoup.Application.StateMachine;
using DuckSoup.Features.InputTest.States;
using DuckSoup.Features.InputTest.Views;
using UnityEngine;

namespace DuckSoup.Bootstrap
{
    public sealed class CompositionRoot : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private InputTestScreenView inputTestScreen;

        private GameStateMachine stateMachine;
        private InputTestState inputTestState;

        private void Awake()
        {
            if (inputTestScreen == null)
            {
                Debug.LogError("[Bootstrap] InputTestScreen no asignada.",this);

                enabled = false;
                return;
            }

            stateMachine = new GameStateMachine();

            inputTestState = new InputTestState(inputTestScreen);

            inputTestScreen.SetInteractionEnabled(false);
        }

        private void OnEnable()
        {
            if (stateMachine == null)
            {
                return;
            }            

            stateMachine.RequestStateChange(inputTestState);
        }

        private void Update()
        {
            stateMachine?.Tick(Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (stateMachine == null)
            {
                return;
            }              

            string previousState = stateMachine.CurrentStateName;

            stateMachine.ApplyPendingTransition();

            string currentState = stateMachine.CurrentStateName;

            if (previousState != currentState)
            {
                Debug.Log($"[FSM] {previousState} -> {currentState}",this);
            }
        }

        private void OnDisable()
        {
            stateMachine?.Stop();
        }
    }
}
