package com.example.viewer

import android.os.Bundle
import android.widget.ToggleButton
import androidx.appcompat.app.AppCompatActivity
import com.example.viewer.R
import com.example.viewer.viewer.ViewerSurfaceView
import com.google.android.material.tabs.TabLayout

class ViewerActivity : AppCompatActivity() {
    private lateinit var surfaceView: ViewerSurfaceView
    private var mode = ViewerMode.CAMERA

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_viewer)

        surfaceView = findViewById(R.id.viewer_surface)
        surfaceView.setMode(mode)

        val tabLayout = findViewById<TabLayout>(R.id.tab_layout)
        tabLayout.addTab(tabLayout.newTab().setText("File").setIcon(R.drawable.ic_file))
        tabLayout.addTab(tabLayout.newTab().setText("View").setIcon(R.drawable.ic_visibility))
        tabLayout.addTab(tabLayout.newTab().setText("Settings").setIcon(R.drawable.ic_settings))

        val toggle = findViewById<ToggleButton>(R.id.mode_toggle_button)
        toggle.setOnCheckedChangeListener { _, isChecked ->
            mode = if (isChecked) ViewerMode.POSE else ViewerMode.CAMERA
            surfaceView.setMode(mode)
        }
    }
}
