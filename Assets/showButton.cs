using UnityEngine;

public class showButton : StateMachineBehaviour
{
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var gameController = FindObjectOfType<GameController>();
        if (gameController != null)
        {
            gameController.showButton();
        }
    }
}
