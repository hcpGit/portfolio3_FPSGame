using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace hcp
{
    public class MoveController
    {
        //  Vector3 moveContRangeMaxVector;
        Vector3 moveContCenter;
        float contRangeMaxSqrMag;
        float contRangeMaxMag;
        float contRangeMaxMagDiv;
        public MoveController(Vector3 moveContCenter, Vector3 moveContRangeMaxPos)
        {
            this.moveContCenter = moveContCenter;

            Vector3 maxContLength = moveContRangeMaxPos - moveContCenter;

            contRangeMaxSqrMag = maxContLength.sqrMagnitude;
            contRangeMaxMag = maxContLength.magnitude;
            contRangeMaxMagDiv = 1 / contRangeMaxMag;
            //  Debug.Log("받은 센터 포지션 = "+ moveContCenter +"받은 맥스 포지션 = " + moveContRangeMaxPos+"맥스까지 벡터="+maxContLength);
        }
        public bool IsInThisContPos(Vector3 inputPos)
        {
            return (inputPos - moveContCenter ).sqrMagnitude <= contRangeMaxSqrMag;
        }

        public Vector3 GetMoveVector(Vector3 currMousePosition, out Vector3 controllerPos)
        {
            Vector3 mousePos = currMousePosition;   //그냥 픽셀 좌표계 벡터 받으면 됨.

            Vector3 fromCenterV = mousePos - moveContCenter;//센터에서 마우스 포지션으로 뻗는 벡터.
            Vector3 dir = fromCenterV.normalized;

            if (contRangeMaxSqrMag < fromCenterV.sqrMagnitude)
            {
                fromCenterV = dir * contRangeMaxMag;
            }

            controllerPos = fromCenterV + moveContCenter;

            Vector3 actualMoveV = dir * (fromCenterV.magnitude * contRangeMaxMagDiv);//센터에서 뻗는 방향 벡터와 콘트롤러 맥스 를 1로 하는 길이값 , 즉 실제 이동할 벡터.
         //   Debug.Log(actualMoveV + "크기는 = " + actualMoveV.magnitude);
         /*
            actualMoveVector.y = 0;
            actualMoveVector.z = actualMoveV.y; //xz 평면 사상.
            actualMoveVector.x = actualMoveV.x;
            */
            return actualMoveV;
        }
    }
}