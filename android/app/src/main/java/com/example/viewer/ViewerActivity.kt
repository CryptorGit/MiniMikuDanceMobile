package com.example.viewer

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.example.viewer.viewer.ViewerSurfaceView

class ViewerActivity : AppCompatActivity() {
    private lateinit var surfaceView: ViewerSurfaceView

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        surfaceView = ViewerSurfaceView(this)
        setContentView(surfaceView)
    }
}
