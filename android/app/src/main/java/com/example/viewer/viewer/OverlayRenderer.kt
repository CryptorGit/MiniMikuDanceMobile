package com.example.viewer.viewer

import android.content.Context
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.util.AttributeSet
import android.view.View
import com.example.viewer.pose.PoseController

class OverlayRenderer @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null
) : View(context, attrs) {
    var poseController: PoseController? = null
    private val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply { color = Color.RED }
    private val selectedPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply { color = Color.YELLOW }

    override fun onDraw(canvas: Canvas) {
        super.onDraw(canvas)
        val controller = poseController ?: return
        val w = width
        val h = height
        controller.getTargets().forEach { target ->
            val (sx, sy) = controller.project(target.position, w, h)
            val p = if (target.selected) selectedPaint else paint
            canvas.drawCircle(sx, sy, 20f, p)
        }
    }
}
