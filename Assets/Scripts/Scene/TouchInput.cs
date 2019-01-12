using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchInput : MonoBehaviour {

    public float ScaleMin = 1.0F;
    public float ScaleMax = 4.0F;
    public float ScaleSpeed = 0.5F;

    Touch[] touches = new Touch[2];
	
	// Update is called once per frame
	void Update () {
        checkTouchInput();
    }

    bool checkTouchInput()
    {
        bool result = false;

        // NOTE zoom and scroll check always
        result = checkZoom() | checkScroll();
        // NOTE check touches only when no other actions
        result = result || checkTouches();

        return result;
    }

    bool checkZoom()
    {
        if (Input.touchCount == 2)
        {
            // zoom scene
            var prevPos0 = Input.touches[0].position - Input.touches[0].deltaPosition;
            var prevPos1 = Input.touches[1].position - Input.touches[1].deltaPosition;

            var prevDistance = Vector2.Distance(prevPos0, prevPos1);
            var currentDistance = Vector2.Distance(Input.touches[0].position, Input.touches[1].position);

            var deltaDistance = prevDistance - currentDistance;

            deltaDistance *= ScaleSpeed * Time.deltaTime;

            transform.localScale -= new Vector3(deltaDistance, deltaDistance, deltaDistance);

            clampZoom();
            clampScroll();

            return true;
        }

        return false;
    }

    bool checkTouches()
    {
        bool result = false;

        for (var i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.touches[i];

            if (!(touch.phase == TouchPhase.Ended && touch.tapCount == 1))
                continue;

#if DEBUG_TOUCH
            Debug.Log("Touch " + touch);
#endif
            result = true;
        }

        return result;
    }

    bool checkScroll()
    {
        bool result = false;

        for (var i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.touches[i];

            //if (touch.phase == TouchPhase.Ended && touch.tapCount == 1)
            // continue;

            if (touch.phase == TouchPhase.Moved)
            {
                var diff = Camera.main.ScreenToWorldPoint(touch.position) - Camera.main.ScreenToWorldPoint(touch.position - touch.deltaPosition);
                transform.position += diff;

                result = true;
                clampScroll();
                break;
            }
        }

        return result;
    }

    void clampZoom()
    {
        // TODO correct min zoom to fit into camera
        transform.localScale = new Vector3(
            Mathf.Clamp(transform.localScale.x, ScaleMin, ScaleMax),
            Mathf.Clamp(transform.localScale.y, ScaleMin, ScaleMax),
            Mathf.Clamp(transform.localScale.z, ScaleMin, ScaleMax)
        );
    }

    void clampScroll()
    {
        // check borders outside of the camera
        Vector3 scaledSize = new Vector3 (Screen.currentResolution.width, Screen.currentResolution.height, 1);
        scaledSize.Scale(transform.localScale);

        var newTransform = transform.position;

        if (scaledSize.x > Camera.main.pixelWidth)
        {
            // check left and right borders
            var leftX = transform.position.x - scaledSize.x * 0.5F;
            var cameraLeftX = Camera.main.transform.position.x - Camera.main.pixelWidth * 0.5F;

            if (leftX > cameraLeftX)
            {
                newTransform.x -= leftX - cameraLeftX;
            }

            var rightX = transform.position.x + scaledSize.x * 0.5F;
            var cameraRightX = Camera.main.transform.position.x + Camera.main.pixelWidth * 0.5F;

            if (rightX < cameraRightX)
            {
                newTransform.x += cameraRightX - rightX;
            }
        }
        else
        {
            // relative to the camera
            newTransform.x = Camera.main.transform.position.x;
        }

        if (scaledSize.y > Camera.main.pixelHeight)
        {
            // check lowY and highY borders
            var lowY = transform.position.y - scaledSize.y * 0.5F;
            var cameraLowY = Camera.main.transform.position.y - Camera.main.pixelHeight * 0.5F;

            if (lowY > cameraLowY)
            {
                newTransform.y -= lowY - cameraLowY;
            }

            var highY = transform.position.y + scaledSize.y * 0.5F;
            var cameraHighY = Camera.main.transform.position.y + Camera.main.pixelHeight * 0.5F;

            if (highY < cameraHighY)
            {
                newTransform.y += cameraHighY - highY;
            }
        }
        else
        {
            // relative to the camera
            newTransform.y = Camera.main.transform.position.y;
        }
        transform.position = newTransform;
    }   
}
