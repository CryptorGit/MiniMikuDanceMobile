package com.example.viewer.camera

import com.example.viewer.math.Vector3
import kotlin.math.sqrt

class CameraController {
    private val position = Vector3(0f, 0f, 10f)
    private val focus = Vector3(0f, 0f, 0f)

    fun rotate(deltaX: Float, deltaY: Float) {
        // TODO: apply rotation to 3D engine
    }

    fun pan(deltaX: Float, deltaY: Float) {
        // TODO: apply panning to 3D engine
    }

    fun zoom(scale: Float) {
        // TODO: apply zoom to 3D engine
    }

    fun getFocus(): Vector3 = focus

    fun distanceToFocus(target: Vector3): Float {
        val dx = target.x - position.x
        val dy = target.y - position.y
        val dz = target.z - position.z
        return sqrt(dx * dx + dy * dy + dz * dz)
    }

    fun screenToWorld(x: Float, y: Float, distance: Float, width: Int, height: Int): Vector3 {
        val nx = (x / width - 0.5f) * distance
        val ny = -(y / height - 0.5f) * distance
        val nz = position.z - distance
        return Vector3(focus.x + nx, focus.y + ny, nz)
    }

    fun project(position: Vector3, width: Int, height: Int): Pair<Float, Float> {
        val sx = width / 2f + (position.x - focus.x)
        val sy = height / 2f - (position.y - focus.y)
        return Pair(sx, sy)
    }
}
