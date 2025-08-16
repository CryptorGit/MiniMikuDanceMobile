package com.example.viewer

import android.os.Bundle
import android.widget.FrameLayout
import android.widget.ImageButton
import android.widget.ToggleButton
import androidx.appcompat.app.AppCompatActivity
import androidx.fragment.app.Fragment
import com.example.viewer.file.ImportPmxFragment
import com.example.viewer.math.Vector3
import com.example.viewer.pose.BoneTarget
import com.example.viewer.pose.PoseController
import com.example.viewer.settings.AppSettingsFragment
import com.example.viewer.viewsettings.ViewSettingsFragment
import com.example.viewer.viewer.OverlayRenderer
import com.example.viewer.viewer.ViewerSurfaceView
import com.google.android.material.bottomsheet.BottomSheetBehavior
import com.google.android.material.tabs.TabLayout

class ViewerActivity : AppCompatActivity() {
    private lateinit var surfaceView: ViewerSurfaceView
    private lateinit var bottomSheetBehavior: BottomSheetBehavior<FrameLayout>
    private var mode = ViewerMode.CAMERA

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_viewer)

        surfaceView = findViewById(R.id.viewer_surface)
        surfaceView.setMode(mode)

        val poseController = PoseController(surfaceView.getCameraController())
        val overlay = findViewById<OverlayRenderer>(R.id.overlay_renderer)
        overlay.poseController = poseController
        poseController.onUpdate = { overlay.invalidate() }
        surfaceView.setPoseController(poseController)
        poseController.setTargets(listOf(BoneTarget("root", Vector3(0f, 0f, 0f))))

        val bottomSheet = findViewById<FrameLayout>(R.id.bottom_sheet)
        bottomSheetBehavior = BottomSheetBehavior.from(bottomSheet).apply {
            state = BottomSheetBehavior.STATE_HIDDEN
            halfExpandedRatio = 0.5f
            isFitToContents = false
        }

        surfaceView.onUserInteraction = {
            bottomSheetBehavior.state = BottomSheetBehavior.STATE_HIDDEN
        }

        val tabLayout = findViewById<TabLayout>(R.id.tab_layout)
        tabLayout.addTab(tabLayout.newTab().setText("File").setIcon(R.drawable.ic_file))
        tabLayout.addTab(tabLayout.newTab().setText("View").setIcon(R.drawable.ic_visibility))
        tabLayout.addTab(tabLayout.newTab().setText("Settings").setIcon(R.drawable.ic_settings))
        tabLayout.addOnTabSelectedListener(object : TabLayout.OnTabSelectedListener {
            override fun onTabSelected(tab: TabLayout.Tab) {
                val fragment: Fragment = when (tab.position) {
                    0 -> ImportPmxFragment()
                    1 -> ViewSettingsFragment()
                    else -> AppSettingsFragment()
                }
                supportFragmentManager.beginTransaction()
                    .replace(R.id.bottom_sheet, fragment)
                    .commit()
                bottomSheetBehavior.state = BottomSheetBehavior.STATE_HALF_EXPANDED
            }

            override fun onTabUnselected(tab: TabLayout.Tab) {}

            override fun onTabReselected(tab: TabLayout.Tab) {
                onTabSelected(tab)
            }
        })

        val toggle = findViewById<ToggleButton>(R.id.mode_toggle_button)
        toggle.setOnCheckedChangeListener { _, isChecked ->
            mode = if (isChecked) ViewerMode.POSE else ViewerMode.CAMERA
            surfaceView.setMode(mode)
            bottomSheetBehavior.state = BottomSheetBehavior.STATE_HIDDEN
        }

        findViewById<ImageButton>(R.id.button_keyframe).setOnClickListener {
            poseController.commitPose()
        }
        findViewById<ImageButton>(R.id.button_add_frame).setOnClickListener {
            // TODO: add frame handling
        }
        findViewById<ImageButton>(R.id.button_remove_frame).setOnClickListener {
            // TODO: remove frame handling
        }
    }
}
