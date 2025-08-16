package com.example.viewer.pose

import com.example.viewer.camera.CameraController
import com.example.viewer.math.Vector3

data class BoneTarget(val name: String, var position: Vector3, var selected: Boolean = false)

class PoseController(private val cameraController: CameraController) {
    private val targets = mutableListOf<BoneTarget>()
    private var dragDistance = 0f
    var onUpdate: (() -> Unit)? = null

    fun setTargets(bones: List<BoneTarget>) {
        targets.clear()
        targets.addAll(bones)
        onUpdate?.invoke()
    }

    fun getTargets(): List<BoneTarget> = targets

    fun selectTargetAt(x: Float, y: Float, width: Int, height: Int) {
        var minIndex = -1
        var minDist = Float.MAX_VALUE
        targets.forEachIndexed { index, target ->
            val (sx, sy) = project(target.position, width, height)
            val dx = sx - x
            val dy = sy - y
            val dist = dx * dx + dy * dy
            if (dist < minDist) {
                minDist = dist
                minIndex = index
            }
        }
        targets.forEachIndexed { index, target ->
            target.selected = index == minIndex
        }
        if (minIndex >= 0) {
            dragDistance = cameraController.distanceToFocus(targets[minIndex].position)
        }
        onUpdate?.invoke()
    }

    fun moveSelected(x: Float, y: Float, width: Int, height: Int) {
        val target = targets.find { it.selected } ?: return
        target.position = cameraController.screenToWorld(x, y, dragDistance, width, height)
        onUpdate?.invoke()
    }

    fun clearSelection() {
        targets.forEach { it.selected = false }
        onUpdate?.invoke()
    }

    fun project(position: Vector3, width: Int, height: Int): Pair<Float, Float> {
        return cameraController.project(position, width, height)
    }

    fun commitPose() {
        // TODO: send current pose to native layer
    }
}
