package com.example.viewer.viewer

import android.content.Context
import android.view.MotionEvent
import android.view.ScaleGestureDetector
import android.view.SurfaceHolder
import android.view.SurfaceView
import com.example.viewer.ViewerMode
import com.example.viewer.camera.CameraController

class ViewerSurfaceView(context: Context) : SurfaceView(context), SurfaceHolder.Callback {
    private val cameraController = CameraController()
    private val scaleDetector = ScaleGestureDetector(context, object : ScaleGestureDetector.SimpleOnScaleGestureListener() {
        override fun onScale(detector: ScaleGestureDetector): Boolean {
            cameraController.zoom(1f / detector.scaleFactor)
            return true
        }
    })

    private var lastX = 0f
    private var lastY = 0f
    private var lastMidX = 0f
    private var lastMidY = 0f

    private var mode = ViewerMode.CAMERA

    var onUserInteraction: (() -> Unit)? = null

    init {
        holder.addCallback(this)
    }

    fun setMode(mode: ViewerMode) {
        this.mode = mode
    }

    override fun onTouchEvent(event: MotionEvent): Boolean {
        onUserInteraction?.invoke()
        return when (mode) {
            ViewerMode.CAMERA -> {
                scaleDetector.onTouchEvent(event)
                when (event.pointerCount) {
                    1 -> handleSingleTouch(event)
                    2 -> handleDoubleTouch(event)
                }
                true
            }
            ViewerMode.POSE -> {
                // TODO: implement IK operations
                true
            }
        }
    }

    private fun handleSingleTouch(event: MotionEvent) {
        when (event.actionMasked) {
            MotionEvent.ACTION_DOWN -> {
                lastX = event.x
                lastY = event.y
            }
            MotionEvent.ACTION_MOVE -> {
                val dx = event.x - lastX
                val dy = event.y - lastY
                cameraController.rotate(-dx, -dy)
                lastX = event.x
                lastY = event.y
            }
        }
    }

    private fun handleDoubleTouch(event: MotionEvent) {
        val midX = (event.getX(0) + event.getX(1)) / 2f
        val midY = (event.getY(0) + event.getY(1)) / 2f
        when (event.actionMasked) {
            MotionEvent.ACTION_POINTER_DOWN -> {
                lastMidX = midX
                lastMidY = midY
            }
            MotionEvent.ACTION_MOVE -> {
                val dx = midX - lastMidX
                val dy = midY - lastMidY
                cameraController.pan(-dx, -dy)
                lastMidX = midX
                lastMidY = midY
            }
        }
    }

    override fun surfaceCreated(holder: SurfaceHolder) {
        // TODO: initialize OpenGL/Vulkan rendering
    }

    override fun surfaceChanged(holder: SurfaceHolder, format: Int, width: Int, height: Int) {
        // TODO: handle size changes
    }

    override fun surfaceDestroyed(holder: SurfaceHolder) {
        // TODO: cleanup resources
    }
}
